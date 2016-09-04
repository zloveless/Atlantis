// -----------------------------------------------------------------------------
//  <copyright file="IServerCommunicator.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    /// <summary>
    ///     <para>Represents a communications link with a game server.</para>
    /// </summary>
    public abstract class ServerCommunicator : IDisposable
    {
        protected readonly string remoteAddress;
        protected readonly int remoteAdminPort;
        protected readonly string remoteAdminPassword;

        protected ServerCommunicator(string remoteAddress, int remoteAdminPort, string remoteAdminPassword)
        {
            this.remoteAddress       = remoteAddress;
            this.remoteAdminPort     = remoteAdminPort;
            this.remoteAdminPassword = remoteAdminPassword;
        }

        #region Methods

        /// <summary>
        ///     <para>Writes a formatted message to the server remote console.</para>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public abstract void Write(string format, params object[] args);

        #endregion
        
        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}
