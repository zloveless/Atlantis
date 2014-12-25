// -----------------------------------------------------------------------------
//  <copyright file="CommandExecuteEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;

    public class CommandExecuteEventArgs : EventArgs
    {
        public CommandExecuteEventArgs(string command, string source, char access, params string[] parameters)
        {
            Access = access;
            Command = command;
            Source = source;
            Parameters = parameters;
        }

        /// <summary>
        ///     Gets the name of the command that was triggered, without the command prefix.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        ///     Gets an array of parameters that were sent with the command.
        /// </summary>
        public string[] Parameters { get; private set; }

        /// <summary>
        ///     Gets the source (n!u@h format) of the command.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the access the source has on the channel.
        /// </summary>
        public char Access { get; private set; }
    }
}
