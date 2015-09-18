// -----------------------------------------------------------------------------
//  <copyright file="IrcClient.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Atlantis.Net.Irc.Parsers;

    public class IrcClient : IrcConnection
    {
        private IDictionary<string, IDictionary<string, string>> _channels = new ConcurrentDictionary<string, IDictionary<string, string>>();

        public IrcClient()
        {
            ModesStringParser = new IrcClientModeParser(this);
            SourceParser = new IrcClientSourceParser();
            ServerInfo = new ServerInfo();
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the current IrcClient has been initialized.
        /// </summary>
        public override bool IsInitialized
        {
            get
            {
                bool result = base.IsInitialized;

                if (string.IsNullOrEmpty(Nick)) result = false;
                else if (string.IsNullOrEmpty(RealName)) result = false;

                return result;
            }
        }

        /// <summary>
        /// Gets or sets a value representing the password for connecting to the server.
        /// </summary>
        public string Password { get; set; }
        public string Nick { get; set; }
        public string Ident { get; set; }
        public string RealName { get; set; }
        
        internal ServerInfo ServerInfo { get; private set; }

        #endregion

        /// <summary>
        ///     <para>This method is called before the main loop is initialized. Use it to send initialization commands to the server.</para>
        ///     <para>There is no need to call the base method when overriding this method.</para>
        /// </summary>
        protected override void OnPreConnect()
        {
            if (!string.IsNullOrEmpty(Password))
            {
                SendImmediately("PASS :{0}", Password);
            }

            SendImmediately("NICK {0}", Nick);
            SendImmediately("USER {0} * :{1}");
        }
    }
}
