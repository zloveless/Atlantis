// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using JetBrains.Annotations;

namespace Atlantis.Net.Irc
{
    public class IrcConfiguration
    {
        /*
        /// <summary>
        ///     <para>Represents the remote host in which the IRC implementation is connecting.</para>
        /// </summary>
        public string Host { get; [UsedImplicitly] private set; }

        /// <summary>
        ///     <para>Represents the remote port in which the IRC implementation is connecting upon.</para>
        /// </summary>
        public ushort Port { get; [UsedImplicitly] private set; }*/

        /// <summary>
        ///     <para>Represents a value that enables a secure sockets layer connection to the remote host.</para>
        /// </summary>
        public bool EnableSsl { get; [UsedImplicitly] private set; } = false;
        
        /*
        /// <summary>
        ///     <para>Represents a value indicating the password to use when negotiating introductions.</para>
        /// </summary>
        public string Password { get; [UsedImplicitly] private set; }*/

        /// <summary>
        ///     <para>Represents the location of the SSL certificate.</para>
        /// </summary>
        public string SslCertificate { get; [UsedImplicitly] private set; }

        /// <summary>
        ///     <para>Represents the location of the SSL certificate private key file.</para>
        /// </summary>
        public string SslCertificateKey { get; [UsedImplicitly] private set; }
    }
}
