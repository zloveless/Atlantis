// -----------------------------------------------------------------------------
//  <copyright file="IrcHeaders.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	public enum IrcHeaders
	{
		// Adding headers as-needed. Will eventually bulk-add them though.

		RPL_WELCOME       = 001,
		RPL_PROTOCTL      = 005,
		RPL_NAMREPLY      = 353,
		RPL_INVITELIST    = 346,
		RPL_EXCEPTLIST    = 348,
		RPL_BANLIST       = 367,
		ERR_NICKNAMEINUSE = 433,
	}
}
