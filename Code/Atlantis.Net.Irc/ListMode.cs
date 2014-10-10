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
		public ListMode(char mode, DateTime date, string mask, string setby)
		{
			Mode  = mode;
			Date  = date;
			Mask  = mask;
			SetBy = setby;
		}

		public DateTime Date { get; private set; }

		public string Mask { get; private set; }

		public char Mode { get; private set; }

		public string SetBy { get; private set; }
	}
}
