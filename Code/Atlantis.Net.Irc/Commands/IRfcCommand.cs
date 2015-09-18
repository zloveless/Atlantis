// -----------------------------------------------------------------------------
//  <copyright file="IRfcCommand.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Commands
{
    public interface IRfcCommand
	{
		/// <summary>
		/// Handles this command using the specified parameters.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parameters"></param>
		void Execute(string source, string[] parameters);
	}
}
