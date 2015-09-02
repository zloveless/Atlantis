// -----------------------------------------------------------------------------
//  <copyright file="NumericExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Extensions
{
    using System;

    public static partial class Extensions
	{
		private static readonly DateTime ORIGIN = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// Converts the specified <see cref="System.Int32" /> to a <see cref="System.DateTime" /> instance.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
	    public static DateTime ToDateTime(this int source)
	    {
	        if (source < 0) throw new ArgumentException("The value cannot be less than zero.", "source");

	        return ORIGIN.AddSeconds(Convert.ToDouble(source));
	    }

        /// <summary>
        /// Converts the specified <see cref="System.Double" /> to a <see cref="System.DateTime" /> instance.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
		public static DateTime ToDateTime(this double source)
		{
			if (source < 0) throw new ArgumentException("The value cannot be less than zero.", "source");

			return ORIGIN.AddSeconds(source);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
		public static double ToTimestamp(this DateTime source)
		{
			return Math.Floor((source - ORIGIN).TotalSeconds);
		}

		public static double ToDouble(this string source)
		{
			return Double.Parse(source);
		}

		public static bool IsNumeric(this Type source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			return (source == typeof (int)
			        || source == typeof (double)
			        || source == typeof (long)
			        || source == typeof (short)
			        || source == typeof (float)
			        || source == typeof (Int16)
			        || source == typeof (Int32)
			        || source == typeof (Int64)
			        || source == typeof (uint)
			        || source == typeof (UInt16)
			        || source == typeof (UInt32)
			        || source == typeof (UInt64)
			        || source == typeof (sbyte)
			        || source == typeof (Single)
				);
		}
	}
}
