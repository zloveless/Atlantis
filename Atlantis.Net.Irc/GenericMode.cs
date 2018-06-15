// -----------------------------------------------------------------------------
//  <copyright file="GenericMode.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class GenericMode
	{
		public char Mode { get; set; }

		public String Parameter { get; set; }

		public bool IsSet { get; set; }

		public String Setter { get; set; }

		public String Target { get; set; }

		public ModeType Type { get; set; }
	}
}
