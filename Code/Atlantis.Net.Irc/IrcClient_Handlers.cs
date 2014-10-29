// -----------------------------------------------------------------------------
//  <copyright file="IrcClient_Handlers.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Atlantis.Linq;
    using Linq;

    public partial class IrcClient
    {
	    #region Fields

	    internal ServerInfo info = new ServerInfo();
	    private bool useExtendedNames;
	    private bool useUserhostNames;
	    private DateTime lastMessage;

	    private String accessRegex;

	    #endregion

        #region Properties

        public bool StrictNames { get; set; }

	    public String PrefixModes
	    {
		    get { return info.PrefixModes; }
	    }

	    public String Prefixes
	    {
		    get { return info.Prefixes; }
	    }

	    #endregion
        
        #region Events Handlers

	    #region Parser

	    protected virtual void OnDataRecv(string line)
	    {
		    lastMessage = DateTime.Now;

		    var tokens = line.Split(' ');
		    var tokenIndex = 0;

		    String source = null;
		    if (tokens[tokenIndex][0] == ':')
		    {
			    // TODO: source parsing
			    source = tokens[tokenIndex].Substring(1);
			    tokenIndex++;
		    }

		    if (tokenIndex == tokens.Length)
		    {
			    // Reached the end.
			    // TODO: maybe disconnect? Idk.
			    return;
		    }

		    var commandName = tokens[tokenIndex++];
		    var parameters = new List<String>();

		    while (tokenIndex != tokens.Length)
		    {
			    if (tokens[tokenIndex][0] != ':')
			    {
				    parameters.Add(tokens[tokenIndex++]);
				    continue;
			    }

			    parameters.Add(String.Join(" ", tokens.Skip(tokenIndex)).Substring(1));
			    break;
		    }

		    int numeric = 0;
		    if (Int32.TryParse(commandName, out numeric))
		    {
			    OnRfcNumeric(numeric, source, parameters.ToArray());
		    }
		    else
		    {
			    OnRfcEvent(commandName, source, parameters.ToArray());
		    }
	    }

	    #endregion

	    protected virtual void OnJoin(String source, String target)
	    {
			String nick = source.GetNickFromSource();
			bool me = Nick.EqualsIgnoreCase(nick);

			JoinEvent.Raise(this, new JoinPartEventArgs(nick, target, me: me));

			if (StrictNames || me)
			{
				Send("NAMES {0}", target);
			}
	    }

	    protected virtual void OnNotice(String source, String target, String message)
	    {
		    NoticeReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));
	    }

	    protected virtual void OnPart(String source, String target, String message)
	    {
			String nick = source.GetNickFromSource();
			bool me = Nick.EqualsIgnoreCase(nick);

		    PartEvent.Raise(this, new JoinPartEventArgs(nick, target, message, me));

			if (StrictNames && !me)
			{
				Send("NAMES {0}", target);
			}

			if (me)
			{
				lock (channels)
				{
					if (channels.ContainsKey(target))
					{ // Remove the channel since we're parting it.
						channels.Remove(target);
					}
				}
			}
	    }

	    protected virtual async void OnPreRegister()
		{
			if (!Connected)
			{
				return;
			}

			if (!String.IsNullOrEmpty(Password))
			{
				await SendNow("PASS {0}", Password);
			}

			await SendNow("NICK {0}", Nick);
			await SendNow("USER {0} 0 * {1}", Ident, RealName.Contains(" ") ? String.Concat(":", RealName) : RealName);

			if (EnableV3)
			{
				await Task.Delay(500);
				await SendNow("CAP LS"); // Request capabilities from the IRC server.
			}
		}

	    protected virtual void OnPrivmsg(String source, String target, String message)
	    {
		    PrivmsgReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));
	    }

	    protected virtual async void OnRfcEvent(String command, String source, String[] parameters)
	    {
		    if (command.EqualsIgnoreCase("PING"))
		    {
			    await SendNow("PONG {0}", parameters[0]);
		    }
			else if (command.EqualsIgnoreCase("CAP"))
			{ // :wolverine.de.cncfps.com CAP 574AAACA9 LS :away-notify extended-join account-notify multi-prefix sasl tls userhost-in-names
				if (!EnableV3) return; // Ignore it. ircv3 spec not enabled.

				var caps = new StringBuilder();
				if (parameters.Any(x => x.EqualsIgnoreCase("multi-prefix")))
				{
					caps.Append("multi-prefix ");
					useExtendedNames = true;
				}
				else if (parameters.Any(x => x.EqualsIgnoreCase("userhost-in-names")))
				{
					caps.Append("userhost-in-names ");
					useUserhostNames = true;
				}

				if (caps.Length > 0)
				{
					await SendNow("CAP REQ :{0}", caps.ToString().Trim(' '));
				}
			}
			else if (command.EqualsIgnoreCase("PRIVMSG"))
			{
				if (parameters.Length < 2)
				{
					Console.WriteLine("Privmsg received with less than 2 parameters.");
				}
				else
				{
					if (source.IsServerSource())
					{
						OnPrivmsg(source, null, String.Join(" ", parameters));
					}
					else
					{
						OnPrivmsg(source, parameters[0], String.Join(" ", parameters.Skip(1)));
					}
				}
			}
			else if (command.EqualsIgnoreCase("NOTICE"))
			{
				if (parameters.Length < 2)
				{
					Console.WriteLine("Notice received with less than 2 parameters.");
				}
				else
				{
					if (source.IsServerSource())
					{
						OnNotice(source, null, String.Join(" ", parameters));
					}
					else
					{
						OnNotice(source, parameters[0], String.Join(" ", parameters.Skip(1)));
					}
				}
			}
			else if (command.EqualsIgnoreCase("JOIN"))
			{
				Debug.Assert(source != null, "Source is null on join.");

				OnJoin(source, parameters[0]);
			}
			else if (command.EqualsIgnoreCase("PART"))
			{
				Debug.Assert(source != null, "Source is null on part.");

				String message = null;
				if (parameters.Length > 1)
				{
					message = String.Join(" ", parameters.Skip(1));
				}

				OnPart(source, parameters[0], message);
			}
		    /*else
			{
				Console.WriteLine("[{0}] Received command from {1} with {2} parameters: {{{3}}}",
					command,
					source,
					parameters.Length,
					String.Join(",", parameters));
			}*/
	    }

	    protected virtual async void OnRfcNumeric(Int32 numeric, String source, String[] parameters)
	    {
		    RfcNumericReceivedEvent.Raise(this, new RfcNumericReceivedEventArgs(numeric, String.Join(" ", parameters)));

			if (numeric == 1)
			{ // Welcome packet
				connectingLock.Release();
				ConnectionEstablishedEvent.Raise(this, EventArgs.Empty);
			}
		    else if (numeric == 5)
		    { // Contribution for handy parsing of 005 courtesy of @aca20031
			    Dictionary<String, String> args = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
			    String[] tokens = parameters.Skip(1).ToArray();
			    foreach (String token in tokens)
			    {
				    int equalIndex = token.IndexOf('=');
				    if (equalIndex >= 0)
				    {
					    args[token.Substring(0, equalIndex)] = token.Substring(equalIndex + 1);
				    }
				    else
				    {
					    args[token] = "";
				    }
			    }

			    if (args.ContainsKey("PREFIX"))
			    {
				    String value = args["PREFIX"];
				    Match m;
				    if (value.TryMatch(@"\(([^\)]+)\)(\S+)", out m))
				    {
					    info.PrefixModes = m.Groups[1].Value;
					    info.Prefixes = m.Groups[2].Value;
				    }
			    }
			    
				if (args.ContainsKey("CHANMODES"))
			    {
				    String[] chanmodes = args["CHANMODES"].Split(',');

				    info.ListModes = chanmodes[0];
				    info.ModesWithParameter = chanmodes[1];
				    info.ModesWithParameterWhenSet = chanmodes[2];
				    info.ModesWithNoParameter = chanmodes[3];
			    }
			    
				if (args.ContainsKey("MODES"))
			    {
				    int modeslen;
				    if (Int32.TryParse(args["MODES"], out modeslen))
				    {
					    info.MaxModes = modeslen;
				    }
			    }

				if (!EnableV3)
				{
					if (args.ContainsKey("NAMESX"))
					{	// Request the server send us extended NAMES (353)
						// This will format a RPL_NAMES using every single prefix the user has on a channel.

						useExtendedNames = true;
						await SendNow("PROTOCTL NAMESX");
					}

					if (args.ContainsKey("UHNAMES"))
					{
						useUserhostNames = true;
						await SendNow("PROTOCTL UHNAMES");
					}
				}
		    }
			else if (numeric == 353)
			{	// note: NAMESX and UHNAMES are not mutually exclusive.
				// NAMESX: (?<prefix>[!~&@%+]*)(?<nick>[^ ]+)
				// UHNAMES: (?<prefix>[!~&@%+]*)(?<nick>[^!]+)!(?<ident>[^@]+)@(?<host>[^ ]+)
				// (?<prefix>[!~&@%+]*)(?<nick>[^!]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?

				if (String.IsNullOrEmpty(accessRegex))
				{
					accessRegex = String.Format(@"(?<prefix>[{0}]*)(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?", Prefixes);
				}

				var c = GetChannel(parameters[2]); // never null.
				var names = String.Join(" ", parameters.Skip(3));
				
				MatchCollection matches;
				if (names.TryMatches(accessRegex, out matches))
				{
					foreach (Match item in matches)
					{ // for now, we only care for the nick and the prefix(es).
						String nick = item.Groups["nick"].Value;

						if (useExtendedNames)
						{
							c.AddOrUpdateUser(nick, item.Groups["prefix"].Value.ToCharArray());
						}
						else
						{
							char prefix = item.Groups["prefix"].Value.Length > 0 ? item.Groups["prefix"].Value[0] : (char)0;
							c.AddOrUpdateUser(nick, new[] {prefix});
						}

						//Console.WriteLine("Found \"{0}\" on channel {1}: {2}", nick, c, item.Groups["prefix"].Value);
					}
				}
			}
			/*else
			{
				Console.WriteLine("[{0:000}] Numeric received with {1} parameters: {{{2}}}",
					numeric,
					parameters.Length,
					String.Join(",", parameters));
			}*/
	    }
		
        #endregion

        #region Callbacks

        protected virtual void WorkerCallback(object state)
        {
            stream = client.GetStream();
            qWorker.Start();

            OnPreRegister(); // Send registration data.

            // TODO: Accept a certificate parameter (read: properly) for sending to the IRC server.
            /*if (Options.HasFlag(ConnectOptions.Secure))
            {
                var ssl = new SslStream(stream, true);
                ssl.AuthenticateAsClient("", new X509CertificateCollection(), SslProtocols.Tls12, false);
            }*/

            reader = new StreamReader(stream, Encoding);

            while (Connected)
            {
                if (stream.DataAvailable)
                {
                    while (!reader.EndOfStream)
                    {
                        String line = reader.ReadLine().TrimIfNotNull();
                        if (!String.IsNullOrEmpty(line))
                        {
                            OnDataRecv(line);
                        }
                    }
                }
            }
        }

        protected virtual void QueueWorkerCallback()
        {
            while (Connected)
            {
                lock (messageQueue)
                {
                    if (messageQueue.Count > 0)
                    {
                        Send(messageQueue.Dequeue());
                    }
                }

                Thread.Sleep(QueueInterval);
            }
        }

        #endregion

        #region Nested type: ServerInfo

        internal class ServerInfo
        {
            /// <summary>
            /// Represents a value indicating what the maximum length the name of a channel can be on the IRC server.
            /// </summary>
            public int ChannelLength { get; set; }

            /// <summary>
            /// Represents a value indicating what types of channels are allowed (channel prefixes)
            /// </summary>
            public String[] ChanTypes { get; set; }

            /// <summary>
            /// Represents the maximum length of a kick comment.
            /// </summary>
            public int KickLength { get; set; }

            /// <summary>
            /// Represents a char array of available list modes on the IRC server.
            /// </summary>
            public String ListModes { get; set; }
            
            /// <summary>
            /// Represents the maximum number of entries that can be set per mode.
            /// </summary>
            public int MaxList { get; set; }
            
            /// <summary>
            /// Represents the maximum number of modes allowed to be set per command.
            /// </summary>
            public int MaxModes { get; set; }

            /// <summary>
            /// Represents a char array of modes that can be un/set on a channel that do not take a parameter (ever).
            /// </summary>
            public String ModesWithNoParameter { get; set; }

            /// <summary>
            /// Represents a char array of modes that can be un/set on a channel that always take a parameter.
            /// </summary>
            public String ModesWithParameter { get; set; }

            /// <summary>
            /// Represents a char array of modes that only take a parameter when being set, no parameter is necessary for unsetting these modes.
            /// </summary>
            public String ModesWithParameterWhenSet { get; set; }
            
            /// <summary>
            /// Represents the list of prefixes available on the server.
            /// </summary>
            public String Prefixes { get; set; }

            /// <summary>
            /// Represents the list of prefix modes associated with each prefix on the server.
            /// </summary>
            public String PrefixModes { get; set; }

            /// <summary>
            /// Represents the maximum length for a channel topic.
            /// </summary>
            public int TopicLength { get; set; }
        }

        #endregion
    }
}
