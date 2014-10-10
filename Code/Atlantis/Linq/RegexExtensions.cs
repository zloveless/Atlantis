// -----------------------------------------------------------------------------
//  <copyright file="StringExExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Linq
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	public static partial class Extensions
	{
		// ReSharper disable InconsistentNaming
		private static readonly Dictionary<string, Regex> regexes = new Dictionary<string, Regex>();
		// ReSharper restore InconsistentNaming

		public static Match Match(this string source, string expression)
		{
			return Regex.Match(source, expression);

			/*Regex r;
			if (regexes.TryGetValue(expression, out r))
			{
				return r.Match(expression);
			}

			var options = RegexOptions.Compiled;
			if (expression.StartsWith("^") || expression.EndsWith("$"))
			{
				options |= RegexOptions.Multiline;
			}

			r = new Regex(expression, options);
			regexes.Add(expression, r);

			Regex.CacheSize += 1;

			return r.Match(expression);*/
		}

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
		///     Attempts a regular expression match on the source string using the expression provided.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="expression"></param>
		/// <param name="match">The matched expression object containing the groups that were matched.</param>
		/// <returns>Returns True or False depending whether the match was successful.</returns>
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
