// -----------------------------------------------------------------------------
//  <copyright file="IrcServer.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    internal class IrcServer : IrcConnection
    {
        /// <summary>
        ///     <para>
        ///         This method is called before the main loop is initialized. Use it to send initialization commands to the
        ///         server.
        ///     </para>
        ///     <para>There is no need to call the base method when overriding this method.</para>
        /// </summary>
        protected override void OnPreConnect()
        {
        }
    }
}
