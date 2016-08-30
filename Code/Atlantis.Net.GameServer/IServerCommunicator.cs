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
    public interface IServerCommunicator : IDisposable
    {
        void Write(string format, params object[] args);
    }
}
