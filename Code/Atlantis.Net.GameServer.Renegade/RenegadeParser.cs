// -----------------------------------------------------------------------------
//  <copyright file="RenegadeParser.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;
    using System.Linq;

    using Atlantis.Extensions;

    public class RenegadeParser : IServerParser, IRenegadeEvents
    {
        /// <summary>
        ///     <para>Gets a value indicating the number of semicolons are in a valid gamelog message.</para>
        /// </summary>
        private const int GameLogValidationCheck = 2;

        #region Event Callbacks

        protected virtual void OnSsgmLog(string header, string message)
        {
            var handler = SsgmLog;
            if (handler != null)
            {
                handler.Raise(this, new SsgmLogEventArgs(message, header));
            }
        }

        protected virtual void OnRenLog(string message)
        {
            var handler = RenegadeLog;
            if (handler != null)
            {
                handler.Raise(this, new RenLogEventArgs(message));
            }
        }

        protected virtual void OnGameLog(string message)
        {
            var handler = GameLog;
            if (handler != null)
            {
                handler.Raise(this, new GameLogEventArgs(message));
            }
        }

        protected virtual void OnConsoleLog(string message)
        {
            var handler = ConsoleLog;
            if (handler != null)
            {
                handler.Raise(this, new RenLogEventArgs(message));
            }
        }

        protected virtual void OnInvalidLog(string message)
        {
            var handler = InvalidLog;
            if (handler != null)
            {
                handler.Raise(this, new RenLogEventArgs(message));
            }
        }

        #endregion
        
        private static string RemoveTimestamp(string str)
        {
            return str.Substring(str.IndexOf(' ') + 1).TrimEnd();
        }

        #region Implementation of IServerParser

        /// <summary>
        ///     <para>Called upon receipt of any message received from the game server.</para>
        /// </summary>
        /// <param name="message"></param>
        public void OnMessage(string message)
        {
            if (message.Length < 3) return;

            int num;
            if (!int.TryParse(message.Substring(0, 3), out num)) return;

            message = message.Substring(3);

            if (num < 3)
            {
                // Blank line
                if (message.Split(' ').Length < 2) return;

                message = RemoveTimestamp(message);
            }

            // num => 0 = SSGM, 1 = GameLog, 2 = RenLog, 3 = Console
            if (num == 0)
            {
                int idx = message.IndexOf(' ');
                if (idx <= 0 || idx >= message.Length) return;

                string header = message.Substring(0, idx);
                string msg = message.Substring(idx + 1);

                // Sometimes gamelog messages can be written to ssgmlog. they're usually semicolon delimited but can also contain other log messages.
                if (header.EqualsIgnoreCase("_GAMELOG") && msg.Count(x => x == ';') > GameLogValidationCheck)
                {
                    OnGameLog(msg);
                }
                else
                {
                    OnSsgmLog(header, msg);
                }
            }
            else if (num == 1)
            {
                
                if (message.Count(x => x == ';') > GameLogValidationCheck)
                {
                    // This shouldn't trigger for SSGM messages being sent down gamelog pipe.
                    OnGameLog(message);
                }
                else
                {
                    // Pass otherwise non-gamelog messages to another handler so the developer can evaluate whether it's erroneous
                    OnInvalidLog(message);
                }
            }
            else if (num == 2)
            {
                OnRenLog(message);
            }
            else
            {
                OnConsoleLog(message);
            }
        }

        #endregion

        #region Implementation of IRenegadeEvents

        public event EventHandler<SsgmLogEventArgs> SsgmLog;
        public event EventHandler<RenLogEventArgs> RenegadeLog;
        public event EventHandler<GameLogEventArgs> GameLog;
        public event EventHandler<RenLogEventArgs> ConsoleLog;
        public event EventHandler<RenLogEventArgs> InvalidLog;

        #endregion
    }
}
