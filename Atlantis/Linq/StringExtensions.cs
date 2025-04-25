// -----------------------------------------------------------------------------
//  <copyright file="StringExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

[PublicAPI]
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
		return source != null ? source.Trim() : string.Empty;
	}

	public static string TrimIfNotNull(this string source, params char[] trimChars)
	{
		return source != null ? source.Trim(trimChars) : string.Empty;
	}

	public static string JoinFormat<T>(this IEnumerable<T> source, string separator, string format)
	{
        // Credit: http://stackoverflow.com/a/13395017
        ArgumentNullException.ThrowIfNull(source);

        format = string.IsNullOrWhiteSpace(format) ? "{0}" : format;
		return string.Join(separator, source.Select(x => string.Format(format, x)));
	}
}
