// -----------------------------------------------------------------------------
//  <copyright file="IrcProtocol.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /*
     * TODO: Implement RFC's.
     * 
     * - https://tools.ietf.org/html/rfc1459 (core)
     * - https://tools.ietf.org/html/rfc2812 (client)
     * - https://tools.ietf.org/html/rfc2813 (server)
     * - http://ircv3.net/irc/ (IRCv3) (main feature: message tags)
     */

    public abstract class IrcProtocol
    {
        protected IrcConnection Connection { get; }

        protected IrcProtocol(IrcConnection connection)
        {
            Connection = connection;
        }

        protected DateTime LastMessage { get; set; }

        /// <summary>
        ///     <para>Handles registration with the connection.</para>
        /// </summary>
        public abstract void RfcRegister();

        /// <summary>
        ///     <para>Called when an RFC command is detected.</para>
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected abstract void OnEvent(string commandName, string source, string[] args);

        /// <summary>
        ///     <para>Called when an RFC numeric is detected.</para>
        /// </summary>
        /// <param name="numeric"></param>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected abstract void OnNumeric(int numeric, string source, string[] args);

        /// <summary>
        ///     <para>Called when a message is received from the connection.</para>
        /// </summary>
        /// <param name="message"></param>
        public virtual void OnMessageReceived(string message)
        {
            LastMessage = DateTime.Now;

            var tokens = message.Split(' ');
            var tokenIndex = 0;

            // TODO: Support IRCv3 tags.

            string source = null;
            if (tokens[tokenIndex][0] == ':')
            {
                source = tokens[tokenIndex].Substring(1);
                tokenIndex++;
            }

            if (tokenIndex == tokens.Length)
            {
                // Nothing to receive.
                return;
            }

            var commandName = tokens[tokenIndex++];
            var parameters = new List<string>();

            while (tokenIndex != tokens.Length)
            {
                if (tokens[tokenIndex][0] != ':')
                {
                    parameters.Add(tokens[tokenIndex++]);
                    continue;
                }

                parameters.Add(string.Join(" ", tokens.Skip(tokenIndex)).Substring(1));
                break;
            }

            int numeric = 0;
            if (int.TryParse(commandName, out numeric))
            {
                OnNumeric(numeric, source, parameters.ToArray());
            }
            else
            {
                OnEvent(commandName, source, parameters.ToArray());
            }
        }
    }
}
