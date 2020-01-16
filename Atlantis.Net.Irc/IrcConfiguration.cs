// -----------------------------------------------------------------------------
//  <copyright file="IrcConfiguration.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Text;

    public struct IrcConfiguration
    {
        public Encoding Encoding { get; set; }

        public String Host { get; set; }

        public int Port { get; set; }

        public bool SslEnabled { get; set; }

        public String Nick { get; set; }

        public String Ident { get; set; }

        public String RealName { get; set; }

        public String Password { get; set; }
    }
}
