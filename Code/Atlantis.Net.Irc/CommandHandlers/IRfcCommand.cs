// -----------------------------------------------------------------------------
//  <copyright file="IRfcCommand.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public interface IRfcCommand
	{
		/// <summary>
		/// Returns the name of the command being executed or a generic identifier in the case of numerics handling.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Handles this command using the specified parameters.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parameters"></param>
		void Handle(string source, string[] parameters);
	}
}
