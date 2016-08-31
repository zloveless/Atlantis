// -----------------------------------------------------------------------------
//  <copyright file="IServerParser.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    /// <summary>
    ///     <para>Represents a generic game server log parser.</para>
    /// </summary>
    public interface IServerParser
    {
        /// <summary>
        ///     <para>Called upon receipt of any message received from the game server.</para>
        /// </summary>
        /// <param name="message"></param>
        void OnMessage(string message);
    }
}
