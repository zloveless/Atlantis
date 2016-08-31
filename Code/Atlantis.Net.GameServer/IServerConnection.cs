// -----------------------------------------------------------------------------
//  <copyright file="IServerConnection.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    /// <summary>
    ///     <para>Represents a logging connection to a game server.</para>
    /// </summary>
    public interface IServerConnection : IDisposable
    {
        /// <summary>
        ///     <para>Gets a value indicating the log parser to be used for the current server connection.</para>
        /// </summary>
        IServerParser Parser { get; }

        /// <summary>
        ///     <para>Establishes a connection to the game server.</para>
        /// </summary>
        void Connect();

        /// <summary>
        ///     <para>Closes a connection to the game server.</para>
        /// </summary>
        void Disconnect();
    }
}
