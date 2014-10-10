// -----------------------------------------------------------------------------
//  <copyright file="ConnectOptions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//      
//      LICENSE TBA
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	[Flags]
	public enum ConnectOptions
	{
		Default = 0,
		Secure = 1,
	}
}