// -----------------------------------------------------------------------------
//  <copyright file="TimeoutEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class TimeoutEventArgs : EventArgs
	{
		public bool Handled { get; set; }
	}
}
