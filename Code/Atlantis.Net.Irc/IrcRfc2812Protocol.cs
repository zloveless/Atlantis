// -----------------------------------------------------------------------------
//  <copyright file="IrcRfc2812Protocol.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    /// <summary>
    ///     <para>Represents the IRC RFC 2812 (Client) Protocol.</para>
    /// </summary>
    public class IrcRfc2812Protocol : IrcProtocol
    {
        public IrcRfc2812Protocol(IrcConnection connection) : base(connection)
        {
        }

        #region Overrides of IrcProtocol

        /// <summary>
        ///     <para>Handles registration with the connection.</para>
        /// </summary>
        public override void RfcRegister()
        {
        }

        /// <summary>
        ///     <para>Called when an RFC command is detected.</para>
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected override void OnEvent(string commandName, string source, string[] args)
        {
        }

        /// <summary>
        ///     <para>Called when an RFC numeric is detected.</para>
        /// </summary>
        /// <param name="numeric"></param>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected override void OnNumeric(int numeric, string source, string[] args)
        {
        }

        /// <summary>
        ///     <para>Called when a message is received from the connection.</para>
        /// </summary>
        /// <param name="message"></param>
        public override void OnMessageReceived(string message)
        {
        }

        #endregion
    }
}
