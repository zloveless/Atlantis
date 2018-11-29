// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Commands
{
    public interface IIrcCommand
    {
        /// <summary>
        ///     <para>The command name according to the protocol, e.g. PRIVMSG, NOTICE.</para>
        /// </summary>
        string Command { get; }

        /// <summary>
        ///     <para>Executes the IRC command.</para>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        void Execute(IrcConnection connection, string source, string[] parameters);
    }
}
