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
        ///     <para>Occurs when a log message was received by the current connection.</para>
        /// </summary>
        event EventHandler<LogMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     <para>Gets a value representing the communication method for the connection.</para>
        /// </summary>
        IServerCommunicator Communicator { get; }
        
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
