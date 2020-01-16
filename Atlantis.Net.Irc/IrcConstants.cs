// -----------------------------------------------------------------------------
//  <copyright file="IrcConstants.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public partial class IrcClient
	{
		#region Reply numerics

		public const Int32 RPL_WELCOME = 001;
		public const Int32 RPL_PROTOCTL = 005;
		public const Int32 RPL_NAMES = 353;

		#endregion
		
		#region Error numerics

		public const Int32 ERR_NAMEINUSE = 433;

		#endregion
	}
}
