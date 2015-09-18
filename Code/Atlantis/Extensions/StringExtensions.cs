// -----------------------------------------------------------------------------
//  <copyright file="StringExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static partial class Extensions
	{
		public static bool EqualsIgnoreCase(this string source, string value)
		{
			return source.Equals(value, StringComparison.OrdinalIgnoreCase);
		}

		public static bool StartsWithIgnoreCase(this string source, string value)
		{
			return source.StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}

		public static string TrimIfNotNull(this string source)
		{
			return source != null ? source.Trim() : String.Empty;
		}

		public static string TrimIfNotNull(this string source, params char[] trimChars)
		{
			return source != null ? source.Trim(trimChars) : String.Empty;
		}

		public static String JoinFormat<T>(this IEnumerable<T> source, String separator, String format)
		{ // Credit: http://stackoverflow.com/a/13395017
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			format = String.IsNullOrWhiteSpace(format) ? "{0}" : format;
			return String.Join(separator, source.Select(x => String.Format(format, x)));
		}
	}
}
