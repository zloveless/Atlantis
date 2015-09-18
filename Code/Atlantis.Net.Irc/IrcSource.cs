// -----------------------------------------------------------------------------
//  <copyright file="IrcSource.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    public struct IrcSource
    {
        public IrcSource(string serverName) : this(null, null, null)
        {
            ServerName = serverName;
        }

        public IrcSource(string nick, string ident, string hostName) : this()
        {
            HostName = hostName;
            Ident = ident;
            Nick = nick;
        }

        public string Nick { get; private set; }
        public string Ident { get; private set; }
        public string HostName { get; private set; }
        public string ServerName { get; private set; }
    }
}
