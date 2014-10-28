// -----------------------------------------------------------------------------
//  <copyright file="IrcClient_Handlers.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Linq;

    public partial class IrcClient
    {
        internal ServerInfo info = new ServerInfo();
        private DateTime lastMessage;
        private bool useExtendedNames = false;

        #region Properties

        public bool StrictNames { get; set; }

        #endregion
        
        #region Events Handlers

        protected virtual void OnDataRecv(string line)
        {
            lastMessage = DateTime.Now;
            String[] toks = line.Split(' ');

            if (toks[0].EqualsIgnoreCase("PING"))
            {
                Send(String.Format("PING {0}", line.Substring(5)));
            }

            int numeric = 0;
            if (Int32.TryParse(toks[1], out numeric))
            {
                OnIrcNumeric(numeric, String.Join(" ", toks.Skip(1).ToArray()));
            }
            else if (toks[1].EqualsIgnoreCase("MODE"))
            {
                 if(StrictNames)
                 { // Request RPL_NAMES for updating the internal permission list.
                    Send("NAMES {0}", toks[2]);
                 }
            }
        }
        
        protected virtual async void OnIrcNumeric(int numeric, String message)
        {
            RfcNumericReceivedEvent.Raise(this, new RfcNumericReceivedEventArgs(numeric, message));

            if (numeric == 1)
            { // Welcome packet
                connectingLock.Release();
                ConnectionEstablishedEvent.Raise(this, EventArgs.Empty);
            }
            else if (numeric == 5)
            { // Contribution for handy parsing of 005 courtesy of @aca20031
                Dictionary<String, String> parameters = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                String[] tokens = message.Split(' ');
                foreach (String token in tokens)
                {
                    int equalIndex = token.IndexOf('=');
                    if (equalIndex >= 0)
                    {
                        parameters[token.Substring(0, equalIndex)] = token.Substring(equalIndex + 1);
                    }
                    else
                    {
                        parameters[token] = "";
                    }
                }

                if (parameters.ContainsKey("PREFIX"))
                {
                    String value = parameters["PREFIX"];
                    Match m;
                    if (value.TryMatch(@"\(([^\)]+)\)(\S+)", out m))
                    {
                        info.PrefixModes = m.Groups[1].Value;
                        info.Prefixes = m.Groups[2].Value;
                    }
                }
                else if (parameters.ContainsKey("CHANMODES"))
                {
                    String[] chanmodes = parameters["CHANMODES"].Split(',');

                    info.ListModes = chanmodes[0];
                    info.ModesWithParameter = chanmodes[1];
                    info.ModesWithParameterWhenSet = chanmodes[2];
                    info.ModesWithNoParameter = chanmodes[4];
                }
                else if (parameters.ContainsKey("MODES"))
                {
                    int modeslen;
                    if (Int32.TryParse(parameters["MODES"], out modeslen))
                    {
                        info.MaxModes = modeslen;
                    }
                }
                else if (parameters.ContainsKey("NAMESX"))
                {
                    // Request the server send us extended NAMES (353)
                    // This will format a RPL_NAMES using every single prefix the user has on a channel.

                    useExtendedNames = true;
                    await SendNow("PROTOCTL NAMESX");
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

                Thread.Sleep(QueueDelay);
            }
        }

        #endregion

        #region Nested type: ServerInfo

        internal class ServerInfo
        {
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

    #region External class: CancelableEventArgs

    public class CancelableEventArgs : EventArgs
    {
        public bool IsCancelled { get; set; }
    }

    #endregion

    #region External class: HandledEventArgs

    public class HandledEventArgs : EventArgs
    {
        public bool IsHandled { get; set; }
    }

    #endregion
}
