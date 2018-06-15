// -----------------------------------------------------------------------------
//  <copyright file="LogThreshold.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.IO
{
	using System;

	[Flags]
	public enum LogThreshold
	{
		None    = 0,
		Info    = 1, // 2^0
		Warning = 2, // 2^1
		Error   = 4, // 2^2
		Fatal   = 8, // 2^3
		Debug   = 16, // 2^4
		Verbose = 31, // Set all bits; 1+2+4+8+16 = 31
	}
}
