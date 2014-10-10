// -----------------------------------------------------------------------------
//  <copyright file="IrcConfiguration.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    public struct IrcConfiguration
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public bool SslEnabled { get; set; }

        public string Nick { get; set; }
        
        public string Ident { get; set; }

        public string RealName { get; set; }

        public string Password { get; set; }
    }
}
