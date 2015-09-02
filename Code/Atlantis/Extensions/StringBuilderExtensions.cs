// -----------------------------------------------------------------------------
//  <copyright file="StringBuilderExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Extensions
{
    using System;
    using System.Text;

    public static partial class Extensions
	{
		public static StringBuilder AppendFormatLine(this StringBuilder source, string format, params object[] args)
		{
			source.AppendFormat(format, args);

			return source.Append(Environment.NewLine);
		}

		public static StringBuilder AppendIf(this StringBuilder source, object item, Predicate<object> condition)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source", "The specified StringBuilder is null.");
			}

			if (condition != null && condition.Invoke(item))
			{
				return source.Append(item);
			}

			return source;
		}

		public static StringBuilder AppendLineIf(this StringBuilder source, string item, Predicate<string> condition)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source", "The specified StringBuilder is null.");
			}

			if (condition != null && condition.Invoke(item))
			{
				return source.AppendLine(item);
			}

			return source;
		}
	}
}
