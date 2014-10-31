// -----------------------------------------------------------------------------
//  <copyright file="ListMode.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class ListMode
	{
		public ListMode(char mode, String mask, String setter, DateTime? date = null)
		{
			Mode  = mode;
			Mask  = mask;
			Setter = setter;
			Date = date.HasValue ? date.Value : DateTime.UtcNow;
		}

		public DateTime Date { get; private set; }

		public String Mask { get; private set; }

		public char Mode { get; private set; }

		public String Setter { get; private set; }
	}
}
