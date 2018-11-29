// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Commands
{
    public interface IIrcNumeric
    {
        /// <summary>
        ///     <para>The numeric according to the protocol, e.g. 001, 005, 353.</para>
        /// </summary>
        int Numeric { get; }

        /// <summary>
        ///     <para>Executes the IRC numeric.</para>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        void Execute(IrcConnection connection, string source, string[] parameters);
    }
}
