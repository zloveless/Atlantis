namespace Atlantis.Linq;

using System;

public static partial class Extensions
{
	private static readonly DateTime EpochTime = new(1970, 1, 1, 0, 0, 0);

	/// <summary>
	/// Converts the specified <see cref="System.Int32" /> to a <see cref="System.DateTime" /> instance.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static DateTime ToDateTime(this int source)
	{
		if (source < 0) throw new ArgumentException("The value cannot be less than zero.", nameof(source));

		return EpochTime.AddSeconds(Convert.ToDouble(source));
	}

	/// <summary>
	/// Converts the specified <see cref="System.Double" /> to a <see cref="System.DateTime" /> instance.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static DateTime ToDateTime(this double source)
	{
		if (source < 0) throw new ArgumentException("The value cannot be less than zero.", nameof(source));

		return EpochTime.AddSeconds(source);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static double ToTimestamp(this DateTime source)
	{
		return Math.Floor((source - EpochTime).TotalSeconds);
	}

	public static double ToDouble(this string source)
	{
		return double.Parse(source);
	}

	public static bool IsNumeric(this Type source)
	{
        ArgumentNullException.ThrowIfNull(source);

        return (source == typeof (int)
		        || source == typeof (double)
		        || source == typeof (long)
		        || source == typeof (short)
		        || source == typeof (float)
		        || source == typeof (short)
		        || source == typeof (int)
		        || source == typeof (long)
		        || source == typeof (uint)
		        || source == typeof (ushort)
		        || source == typeof (uint)
		        || source == typeof (ulong)
		        || source == typeof (sbyte)
		        || source == typeof (float)
			);
	}
}
