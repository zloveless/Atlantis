// -----------------------------------------------------------------------------
//  <copyright file="IrcClientEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class JoinPartEventArgs : EventArgs
	{
		public JoinPartEventArgs(string nick, string channel, String message = null, bool me = false)
		{
			Channel = channel;
			Nick = nick;

			Message = message;
			IsMe = me;
		}

		public bool IsMe { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the name of the user that joined or parted the specified channel.
		/// </summary>
		public string Nick { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the target of the join or part event.
		/// </summary>
		public string Channel { get; private set; }

		/// <summary>
		///		<para>Gets a <see cref="T:System.String" /> value representing the client part message received.</para>
		///		<para>This will always be null on join.</para>
		/// </summary>
		public String Message { get; private set; }
	}
}
