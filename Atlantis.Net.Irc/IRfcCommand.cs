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
		String Name { get; }

		/// <summary>
		/// Handles this command using the specified parameters.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parameters"></param>
		void Handle(String source, String[] parameters);
	}

	public abstract class RfcCommand : IRfcCommand
	{
		protected readonly IrcClient origin;

		protected RfcCommand(IrcClient origin)
		{
			this.origin = origin;
		}

		#region Implementation of IRfcCommand

		/// <summary>
		/// Returns the name of the command being executed or a generic identifier in the case of numerics handling.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Handles this command using the specified parameters.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parameters"></param>
		public abstract void Handle(string source, string[] parameters);

		#endregion
	}
}
