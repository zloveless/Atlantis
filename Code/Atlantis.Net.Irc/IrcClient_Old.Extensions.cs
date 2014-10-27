// -----------------------------------------------------------------------------
//  <copyright file="IrcClientExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public partial class IrcClient_Old
	{
		public bool IsHigherOrEqualToPrefix(char prefixA, char prefixB)
		{
			if (prefixA == prefixB) return true;

			char[] accessPrefixes = AccessPrefixes.ToCharArray();
			Array.Reverse(accessPrefixes);

			String prefixes = new string(accessPrefixes);
			return prefixes.IndexOf(prefixA) >= prefixes.IndexOf(prefixB);
		}

		public Int32 ComparePrefix(Char left, Char right)
		{
			if (left == right) return 0;

			char[] accessPrefixes = AccessPrefixes.ToCharArray();
			Array.Reverse(accessPrefixes);

			String prefixes = new string(accessPrefixes);

			return prefixes.IndexOf(left) > prefixes.IndexOf(right) ? 1 : -1;
		}
	}

	public static class Extensions
	{
		/*
		public enum IrcColor
		{
			White      = 0,   /**< White #1#
			Black      = 1,   /**< Black #1#
			DarkBlue   = 2,   /**< Dark blue #1#
			DarkGreen  = 3,   /**< Dark green #1#
			Red        = 4,   /**< Red #1#
			DarkRed    = 5,   /**< Dark red #1#
			DarkViolet = 6,   /**< Dark violet #1#
			Orange     = 7,   /**< Orange #1#
			Yellow     = 8,   /**< Yellow #1#
			LightGreen = 9,   /**< Light green #1#
			Cyan       = 10,   /**< Cornflower blue #1#
			LightCyan  = 11,   /**< Light blue #1#
			Blue       = 12,   /**< Blue #1#
			Violet     = 13,   /**< Violet #1#
			DarkGray   = 14,   /**< Dark gray #1#
			LightGray  = 15   /**< Light gray #1#
		}

		public enum ControlCode
		{
			Bold          = 0x02,     /**< Bold #1#
			Color         = 0x03,     /**< Color #1#
			Italic        = 0x09,     /**< Italic #1#
			StrikeThrough = 0x13,     /**< Strike-Through #1#
			Reset         = 0x0f,     /**< Reset #1#
			Underline     = 0x15,     /**< Underline #1#
			Underline2    = 0x1f,     /**< Underline #1#
			Reverse       = 0x16      /**< Reverse #1#
		}*/

		public static String Color(this String source, Int32 color)
		{
			return String.Concat("\x03", color, source, "\x03");
		}

		public static String Bold(this String source)
		{
			return String.Concat("\x02", source, "\x02");
		}
	}

}
