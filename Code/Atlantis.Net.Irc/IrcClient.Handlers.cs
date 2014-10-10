// -----------------------------------------------------------------------------
//  <copyright file="IrcClient.Callbacks.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Timers;
	using Linq;
	using Timer = System.Timers.Timer;

	public partial class IrcClient
	{
		#region Fields

		internal string accessModes;
		internal string accessRegex;
		internal string[] channelModes;

		private bool useExtendedNames;

		private bool registered;
		private DateTime lastMessage;
		private Timer timeoutTimer;
	    private Timer pingTimer;
		private string rfcStringCase;

		// PRIVMSG|NOTICE|JOIN|PART|QUIT|MODE|NICK|INVITE|KICK
		// private const string IRC_PROTOSTR = @"^:?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+)) (?<command>[A-Z]) :?(?<target>\#?[^\W]+)\W?:?(?<params>.+)?$";

		private const string IRC_USERSTR     = @":?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+))";
		private const string IRC_CHANEX      = @":?(?<target>\#?[^\W]+)";

		#endregion

		#region Properties

		public string AccessPrefixes { get; private set; }

		#endregion


		// TODO: Handle disconnection events.
		// TODO: DEBUG RECV: ERROR :Closing link: (lantea@1.2.3.4) [Killed (Genesis2001 (foo))]

		private void TickTimeout()
		{
            if (pingTimer != null) pingTimer.Dispose();
            if (timeoutTimer != null) timeoutTimer.Dispose();

            pingTimer = new Timer(Timeout.TotalMilliseconds / 2);
		    pingTimer.Elapsed += SendPingPacket;
            pingTimer.Start();

			timeoutTimer          = new Timer(Timeout.TotalMilliseconds);
			timeoutTimer.Elapsed += OnTimeoutTimerElapsed;
			timeoutTimer.Start();
		}

		#region Handlers

		#region IRC Numeric Handlers

		protected virtual void ConnectionHandler(object sender, RfcNumericEventArgs args)
		{
			var header = (IrcHeaders)args.Numeric;

			if (header == IrcHeaders.RPL_WELCOME)
			{
				ConnectionEstablishedEvent.Raise(this, EventArgs.Empty);
				registered = true;
			}
		}
		
		protected virtual void RfcProtocolHandler(object sender, RfcNumericEventArgs args)
		{
			var header  = (IrcHeaders)args.Numeric;
			var message = args.Message;

			if (header == IrcHeaders.RPL_PROTOCTL)
			{
				Match m;
				if (message.TryMatch(@"PREFIX=\((\S+)\)(\S+)", out m))
				{
					accessModes    = m.Groups[1].Value;
					AccessPrefixes = m.Groups[2].Value;
				}
				
				if (message.TryMatch(@"CHANMODES=(\S+)", out m))
				{
					channelModes = m.Groups[1].Value.Split(',');
				}
				
				if (message.TryMatch(@"CASEMAPPING=([a-z\-])", out m))
				{
					rfcStringCase = m.Groups[1].Value;
				}
				
				if (message.TryMatch(@"MODES=(\d+)", out m))
				{
					// maximum modes per set request.
				}
				
				// TODO: Maybe implement UHNAMES handling? For the moment, we just care about NAMESX. :)
				if (message.Contains("NAMESX"))
				{
					Send("PROTOCTL NAMESX");
					useExtendedNames = true;
				}
			}
		}

		protected virtual void RfcNamesHandler(object sender, RfcNumericEventArgs args)
		{
			var header  = (IrcHeaders)args.Numeric;
			var message = args.Message;

			if (header == IrcHeaders.RPL_NAMREPLY)
			{
				var toks   = message.Split(' ');
				var c      = GetChannel(toks[2]);
				var names  = message.Substring(message.IndexOf(':') + 1);

				MatchCollection collection;
				if (string.IsNullOrEmpty(accessRegex))
				{
					accessRegex = string.Format(@"(?<prefix>[{0}]*)(?<nick>\S+)", AccessPrefixes);
				}

				if (names.TryMatches(accessRegex, out collection))
				{
					foreach (Match item in collection)
					{
						string nick = item.Groups["nick"].Value;
						PrefixList list;
						if (!c.Users.TryGetValue(nick, out list))
						{
							list = new PrefixList(this);
							c.Users.Add(nick, list);
						}

						if (useExtendedNames)
						{
							string prefixes = item.Groups["prefix"].Value;
							
							foreach (char prefix in prefixes)
							{
								c.Users[nick].AddPrefix(prefix);
							}
						}
						else
						{
							char prefix = item.Groups["prefix"].Value.Length > 0 ? item.Groups["prefix"].Value[0] : (char)0;

							c.Users[nick].AddPrefix(prefix);
						}
					}
				}
			}
		}

		protected virtual void ListModeHandler(object sender, RfcNumericEventArgs args)
		{
			var header  = (IrcHeaders)args.Numeric;

			// CHANMODES=Ibeg,k,FHJLdfjl,ABCDGKMNOPQRSTcimnprstuz 
			
			if (header == IrcHeaders.RPL_BANLIST || header == IrcHeaders.RPL_EXCEPTLIST || header == IrcHeaders.RPL_INVITELIST)
			{
				string[] toks      = args.Message.Split(' ');
				string channelName = toks[1];
				string mask        = toks[2];
				string whomSet     = toks[3];
				DateTime set       = toks[4].ToDouble().ToDateTime();

				var c    = GetChannel(channelName);
				var type = '\0';

				switch (header)
				{
					case IrcHeaders.RPL_BANLIST:
						type = 'b';
						break;
					
					case IrcHeaders.RPL_EXCEPTLIST:
						type = 'e';
						break;

					case IrcHeaders.RPL_INVITELIST:
						type = 'I';
						break;
				}

				if (c.ListModes.Any(x => x.Mask.Equals(mask)))
				{
					return;
				}

				var l = new ListMode(type, set, mask, whomSet);
				c.ListModes.Add(l);
			}
		}

		protected virtual void NickInUseHandler(object sender, RfcNumericEventArgs args)
		{
			var header  = (IrcHeaders)args.Numeric;
			
			if (header == IrcHeaders.ERR_NICKNAMEINUSE)
			{
				var message = args.Message;
				var toks    = message.Split(' ');
				var newNick = string.Concat(toks[1], "_");

				ChangeNick(newNick);

				if (RetryNick)
				{
					Task.Factory.StartNew(() =>
					                      {
						                      if (!registered)
						                      {
							                      Task.Delay((int)RetryInterval, token).Wait(token);
						                      }

						                      ChangeNick(Nick);
					                      }, token);
				}
			}
		}

		#endregion

		#region IRC Raw Message Handlers

		/*protected virtual void ProtocalMessageReceivedHandler(object sender, RawMessageEventArgs args)
		{
			var message = args.Message;
			Match m;
			// Regular Expression Credits
			// created by Chris J. Hogben (http://cjh.im/)
			// modified by Zack Loveless (http://zloveless.com)

			// if (message.TryMatch(@"^:?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+)) (?<command>PRIVMSG|NOTICE|JOIN|PART|QUIT|MODE|NICK) :?(?<target>\#?[^\W]+)\W?:?(?<params>.+)?$", out m))
			if (message.TryMatch(@"^:?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+)) (?<command>[A-Z]) :?(?<target>\#?[^\W]+)\W?:?(?<params>.+)?$", out m))
			{
				ProtocolMessageReceivedEvent.Raise(this, new ProtocolMessageEventArgs(m, message));
			}
		}*/

		protected virtual void QuitHandler(object sender, RawMessageEventArgs args)
		{
			string[] toks = args.Tokens;

			if (toks[1].Equals("QUIT"))
			{
				Match m;
				if (toks[0].TryMatch(IRC_USERSTR, out m))
				{
					string source  = m.Groups["source"].Value;
					string message = string.Join(" ", toks.Skip(2)).TrimStart(':');

					foreach (Channel c in Channels)
					{
						if (c.Users.ContainsKey(source))
						{
							c.Users.Remove(source);
						}
					}

					QuitEvent.Raise(this, new QuitEventArgs(source, message));
				}
			}
		}

		protected virtual void JoinPartHandler(object sender, RawMessageEventArgs args)
		{
			var toks    = args.Tokens;
			
			if (toks[1].Equals("JOIN") || toks[1].Equals("PART"))
			{
				// :Lantea!lantea@unified-nac.jhi.145.98.IP JOIN :#UnifiedTech

				Match m;
				if (toks[0].TryMatch(IRC_USERSTR, out m))
				{
					string source = m.Groups["source"].Value;
					string target = null;

					Match n;
					if (toks[2].TryMatch(IRC_CHANEX, out n))
					{
						target = n.Groups["target"].Value;
					}

					if (target != null)
					{
						Channel channel = GetChannel(target);

						if (toks[1].Equals("JOIN"))
						{
							if (!channel.Users.ContainsKey(source))
							{
								channel.Users.Add(source, new PrefixList(this));
							}

							ChannelJoinEvent.Raise(this, new JoinPartEventArgs(source, target));

							if (FillListsOnJoin && source.EqualsIgnoreCase(Nick))
							{
								if (FillListsDelay > 0)
								{
									try
									{
										Task.Factory.StartNew(() =>
										                      {
											                      Task.Delay((int)FillListsDelay, token).Wait(token);

											                      FillChannelList(target);
										                      },
											token);
									}
									catch (TaskCanceledException)
									{
										// Omnomnomnom
									}
								}
								else
								{
									FillChannelList(target);
								}
							}
						}
						else if (toks[1].Equals("PART"))
						{
							if (channel.Users.ContainsKey(source))
							{
								channel.Users.Remove(source);
							}

							ChannelPartEvent.Raise(this, new JoinPartEventArgs(source, target));
						}

						if (StrictNames)
						{
							if (RequestDelay > 0)
							{
								Task.Factory.StartNew(() =>
								                      {
									                      Task.Delay((int)RequestDelay, token).Wait(token);

									                      Send("NAMES {0}", target);
								                      }, token);
							}
							else
							{
								Send("NAMES {0}", target);
							}
						}
					}
				}
			}
		}

		private void FillChannelList(string channelName)
		{
			if (string.IsNullOrEmpty(channelName)) throw new ArgumentNullException("channelName");

			var modes = channelModes[0];
			foreach (char mode in modes)
			{
				Send("MODE {0} +{1}", channelName, mode);
			}
		}

		protected virtual void MessageNoticeHandler(object sender, RawMessageEventArgs args)
		{
			string[] toks = args.Tokens;

			// ^:?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+)) (?<command>[A-Z]) :?(?<target>\#?[^\W]+)\W?:?(?<params>.+)?$

			if (toks[1].Equals("PRIVMSG") || toks[1].Equals("NOTICE"))
			{
				Match m;
				if (toks[0].TryMatch(IRC_USERSTR, out m))
				{
					string source = m.Groups["source"].Value;
					string target = null;

					Match n;
					if (toks[2].TryMatch(IRC_CHANEX, out n))
					{
						target = n.Groups["target"].Value;
					}

					if (target != null)
					{
						string message = string.Join(" ", toks.Skip(3));

						if (message.StartsWith(":")) message = message.Substring(1);

						if (toks[1].Equals("PRIVMSG"))
						{
							MessageReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));
						}
						else if (toks[1].Equals("NOTICE"))
						{
							NoticeReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));
						}

						// TODO: Add support for NOTICE AUTH messages.
					}
				}
				else
				{
					string source  = toks[0].TrimStart(':');
					string message = string.Join(" ", toks.Skip(3)).TrimStart(':');

					ServerNoticeReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, null, message));
				}
			}
		}
		
		protected virtual void ModeHandler(object sender, RawMessageEventArgs args)
		{
			string[] toks = args.Tokens;

			if (toks[1].Equals("MODE"))
			{
				// ^:?(?<source>[^!]+)\!((?<ident>[^@]+)@(?<host>\S+)) (?<command>[A-Z]) :?(?<target>\#?[^\W]+)\W?:?(?<params>.+)?$

				Match m;
				if (toks[0].TryMatch(IRC_USERSTR, out m))
				{
					string source = m.Groups["source"].Value;
					string target = null;

					Match n;
					if (toks[2].TryMatch(IRC_CHANEX, out n))
					{
						target = n.Groups["target"].Value;
					}

					if (target != null)
					{
						string modeList  = toks[3].TrimStart(':');
						string[] data = toks.Skip(4).ToArray();

						bool set = false;
						
						Channel channel = null;
						if (target.StartsWith("#"))
						{
							channel = GetChannel(target);
						}

						bool accessList = false;

						// TODO: Add error checking to parameter list.
						for (int i = 0; i < modeList.Length; ++i)
						{
							if (modeList[i]      == '+') set = true;
							else if (modeList[i] == '-') set = false;
							else if (channel  == null)
							{
								modes.Add(modeList[i]);
							}
							else if (channelModes[0].Contains(modeList[i]))
							{
								if (!channel.ListModes.Any(x => x.Mask.Equals(data[i - 1])) && set)
								{
								    channel.ListModes.Add(new ListMode(modeList[i], DateTime.Now, data[i - 1], source));
								}
								else if (channel.ListModes.Any(x => x.Mask.Equals(data[i - 1])) && !set)
								{
								    ListMode tmp = channel.ListModes.SingleOrDefault(x => x.Mask.Equals(data[i - 1]));

								    if (tmp != null)
								    {
								        channel.ListModes.Remove(tmp);
								    }
								}
							}
							else if (channelModes[1].Contains(modeList[i]))
							{
								// mode that always has a parameter
							    if (channel.Modes.Any(x => x.Key.Equals(modeList[i]) && x.Value.Equals(data[i - 1])))
							    {
							        channel.Modes.Remove(modeList[i]);
							        channel.Modes.Add(modeList[i], data[i - 1]);
							    }
							}
							else if (channelModes[2].Contains(modeList[i]))
							{
								// mode that only has a parameter when being set
							}
							else if (channelModes[3].Contains(modeList[i]))
							{
                                if (set && !channel.Modes.ContainsKey(modeList[i]))
                                {
                                    channel.Modes.Add(modeList[i], String.Empty);
                                }
                                else if (!set && channel.Modes.ContainsKey(modeList[i]))
                                {
                                    channel.Modes.Remove(modeList[i]);
                                }
                                else
                                {
                                    ConsoleColor c          = Console.ForegroundColor;
                                    Console.ForegroundColor = ConsoleColor.Yellow;

                                    Console.WriteLine("DEBUG: {0} was {1}set on channel {2} at {3}",
                                        modeList[i],
                                        !set ? "un" : "",
                                        channel,
                                        DateTime.Now);

                                    Console.ForegroundColor = c;
                                }
							}
							else if (accessModes.Contains(modeList[i]))
							{
								PrefixList list;
								if (!channel.Users.TryGetValue(data[i - 1], out list))
								{
									list = new PrefixList(this);
									channel.Users.Add(source, list);
								}

								int pi      = accessModes.IndexOf(modeList[i]);
								char prefix = AccessPrefixes[pi];

								if (set) list.AddPrefix(prefix);
								else list.RemovePrefix(prefix);

								accessList = true;
							}
						}

						if (accessList)
						{
							if (StrictNames)
							{
								Send("NAMES {0}", target);
							}
						}
					}
				}
			}
		}

		protected virtual void NickHandler(object sender, RawMessageEventArgs args)
		{
			string[] toks = args.Tokens;

			if (toks[1].Equals("NICK"))
			{
				Match m;
				if (toks[0].TryMatch(IRC_USERSTR, out m))
				{
					string source = m.Groups["source"].Value;
					string target = toks[2].TrimStart(':');

					foreach (Channel c in Channels)
					{
						if (c.Users.ContainsKey(source))
						{
							var p = c.Users[source];

							c.Users.Remove(source);
							c.Users.Add(target, p);
						}
					}

					NickChangedEvent.Raise(this, new NickChangeEventArgs(source, target));
				}
			}
		}

		protected virtual void RfcNumericHandler(object sender, RawMessageEventArgs args)
		{
			var toks = args.Tokens;

			int num;
			if (Int32.TryParse(toks[1], out num))
			{
				var message = string.Join(" ", toks.Skip(2));
				RfcNumericEvent.Raise(this, new RfcNumericEventArgs(num, message));
			}
		}

		protected virtual void RegistrationHandler(object sender, RawMessageEventArgs args)
		{
			if (registered) return;
			if (!string.IsNullOrEmpty(Password)) Send("PASS :{0}", Password);

			// TODO: Check for NOTICE AUTH ...
			// Accident waiting to happen I think.

			// TODO: Handle RFC numeric 464 too for this.

			Send("NICK {0}", Nick);
			Send("USER {0} 0 * :{1}", Ident, RealName);

			RawMessageReceivedEvent -= RegistrationHandler;
			registered = true;
		}

		protected virtual void PingHandler(object sender, RawMessageEventArgs args)
		{
			if (args.Message.StartsWith("PING"))
			{
				// Bypass the queue for sending pong responses.
				Send(string.Format("PONG {0}", args.Message.Substring(5)));
				PingReceiptEvent.Raise(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Callbacks

		private async void OnAsyncRead(Task<String> task, object state)
		{
			if (task.Exception == null && task.Result != null && !task.IsCanceled)
			{
				lastMessage = DateTime.Now;
				RawMessageReceivedEvent.Raise(this, new RawMessageEventArgs(task.Result));

				await
					reader.ReadLineAsync().
						ContinueWith(OnAsyncRead, state, token, TaskContinuationOptions.LongRunning, TaskScheduler.Current);
			}
            else if (task.Exception != null)
            {
                // TODO: Alert user.
                client.Close();
            }
			else if (task.Result == null)
			{
				client.Close();
			}
		}
		
		protected async void QueueProcessor(object o)
		{
			try
			{
				while (client != null && client.Connected)
				{
					if (messageQueue.Count > 0)
					{
						Send(messageQueue.Pop());
					}

					await Task.Delay(QueueInteval, token);
				}
			}
			catch (TaskCanceledException)
			{
				// nom nom.
			}
		}

		private void CancellationNoticeHandler()
		{
			if (!tokenSource.IsCancellationRequested) return;

			Send("QUIT :Exiting.");
			client.Close();
		}

	    private void SendPingPacket(object sender, ElapsedEventArgs e)
	    {
	        Send("PING :{0}", Host);
	    }

		private async void OnTimeoutTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if ((e.SignalTime - lastMessage) > Timeout)
			{
				var args = new TimeoutEventArgs();
				TimeoutEvent.Raise(this, args);

                tokenSource.Dispose();

				if (!args.Handled)
				{
					tokenSource = new CancellationTokenSource();
					token       = tokenSource.Token;

					if (RetryInterval > 0)
					{
						// ReSharper disable MethodSupportsCancellation
						await Task.Delay((int)RetryInterval);
						// ReSharper restore MethodSupportsCancellation
					}

					Connect();
				}
			}
		}

		#endregion

		#endregion
	}
}
