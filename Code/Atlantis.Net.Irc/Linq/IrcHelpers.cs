// -----------------------------------------------------------------------------
//  <copyright file="IrcHelpers.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Linq
{
	using System;
	using System.Text.RegularExpressions;

	using Atlantis.Extensions;

    public static class IrcHelpers
	{
		public static String GetNickFromSource(this String source)
		{
			Match m;
			return source.TryMatch(@"(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?", out m)
				? m.Groups["nick"].Value
				: source;
		}

		public static String GetIdentFromSource(this String source)
		{
			Match m;
			if (!source.TryMatch(@"(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?", out m))
			{
				return source;
			}
			
			var ident = m.Groups["ident"];
			return ident.Success ? ident.Value : source;
		}

		public static String GetHostFromSource(this String source)
		{
			Match m;
			if (!source.TryMatch(@"(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?", out m))
			{
				return source;
			}

			var host = m.Groups["host"];
			return host.Success ? host.Value : source;
		}

		public static bool IsUserSource(this String source)
		{
			return source.Matches(@"(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?");
		}

		public static bool IsServerSource(this String source)
		{
			return !source.Matches(@"(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?");
		}
	}
}
