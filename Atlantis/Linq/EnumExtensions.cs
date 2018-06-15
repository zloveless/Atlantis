// -----------------------------------------------------------------------------
//  <copyright file="EnumExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

#if NET452
namespace Atlantis.Linq
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	public static partial class Extensions
	{
		public static string GetDescription(this Enum source)
		{
            // TODO: This doesn't work for .NET Standard 1.4.x. Fix it.
			if (source == null) throw new ArgumentNullException(nameof(source));

			// http://weblogs.asp.net/grantbarrington/archive/2009/01/19/enumhelper-getting-a-friendly-description-from-an-enum.aspx#7801000
			return (from m in source.GetType().GetMember(source.ToString())
				let attr = (DescriptionAttribute)m.GetCustomAttributes(typeof (DescriptionAttribute), false).FirstOrDefault()
				select attr == null ? source.ToString() : attr.Description).FirstOrDefault();
		}
	}
}
#endif