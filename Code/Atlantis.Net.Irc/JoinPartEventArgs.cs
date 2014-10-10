// -----------------------------------------------------------------------------
//  <copyright file="JoinPartEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------
namespace Atlantis.Net.Irc
{
	using System;

	public class JoinPartEventArgs : EventArgs
	{
		public JoinPartEventArgs(string nick, string channel)
		{
			Channel = channel;
			Nick = nick;
		}

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the name of the user that joined or parted the specified channel.
		/// </summary>
		public string Nick { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the target of the join or part event.
		/// </summary>
		public string Channel { get; private set; }
	}
}
