// -----------------------------------------------------------------------------
//  <copyright file="ModeType.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	public enum ModeType
	{
		/// <summary>
		///     Channel mode has many separate parameters which can be by requesting +[x] where [x] is the mode with no parameters.
		/// </summary>
		LIST,

		/// <summary>
		///     Channel mode that always takes a parameter, regardless whether it's set or unset.
		/// </summary>
		SETUNSET,

		/// <summary>
		///     Channel mode that requires a parameter only when being set, otherwise has no parameter.
		/// </summary>
		SET,

		/// <summary>
		///     Channel mode should never have a parameter associated with it.
		/// </summary>
		NOPARAM,

		/// <summary>
		///     Mode that grants a user access on a channel. The associated prefix should be stored with the user, not the channel.
		/// </summary>
		ACCESS,

		/// <summary>
		///     Generic mode representing a user mode that should be stored for the IrcClient (ourselves). Occurs when target and
		///     source are the same value.
		/// </summary>
		USER
	}
}
