// -----------------------------------------------------------------------------
//  <copyright file="StringExExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Extensions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     <para>Contains methods used to match strings against a given regular expression.</para>
    ///     <para>Makes use of an internal dictionary for caching commonly used regular expressions.</para>
    /// </summary>
	public static class RegexExtensions
	{
		// ReSharper disable InconsistentNaming
		private static readonly Dictionary<string, Regex> regexes = new Dictionary<string, Regex>();
		// ReSharper restore InconsistentNaming

        /// <summary>
        /// Attempts to match the specified string against the specified expression
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expression"></param>
        /// <returns>Returns the result of the match for manipulation</returns>
		public static Match Match(this string source, string expression)
		{
			return Regex.Match(source, expression);
		}

        /// <summary>
        /// Performs a quick match of the specified string to the given expression.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expression"></param>
        /// <returns>Returns a boolean value representing whether the specified string matches the given expression</returns>
		public static bool Matches(this string source, string expression)
		{
			try
			{
				return Regex.IsMatch(source, expression);
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
		}

		/// <summary>
        /// Performs a regular expression match on the specified string to the given expression.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="expression"></param>
		/// <param name="match">The matched expression object containing the groups that were matched.</param>
		/// <returns>Returns a boolean value representing whether the specified string matches the given expression.</returns>
		public static bool TryMatch(this string source, string expression, out Match match)
		{
			Regex r;
			if (regexes.TryGetValue(expression, out r))
			{
				match = r.Match(source);
				return match.Success;
			}

			var options = RegexOptions.Compiled;
			if (expression.StartsWith("^") || expression.EndsWith("$"))
			{
				options |= RegexOptions.Multiline;
			}

			r = new Regex(expression, options);
			regexes.Add(expression, r);

			Regex.CacheSize += 1;

			match = r.Match(source);
			return match.Success;
		}

        /// <summary>
        /// Performs multiple searches within a specified string against a given expression to return multiple matches.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expression"></param>
        /// <param name="match">The matched expression objects containing the groups that were matched.</param>
        /// <returns>Returns a boolean value representing whether the specified string matches the given expression.</returns>
		public static bool TryMatches(this string source, string expression, out MatchCollection match)
		{
			Regex r;
			if (regexes.TryGetValue(expression, out r))
			{
				match = r.Matches(source);
				return match.Count > 0;
			}

			var options = RegexOptions.Compiled;
			if (expression.StartsWith("^") || expression.EndsWith("$"))
			{
				options |= RegexOptions.Multiline;
			}

			r = new Regex(expression, options);
			regexes.Add(expression, r);

			Regex.CacheSize += 1;

			match = r.Matches(source);
			return match.Count > 0;
		}
	}
}
