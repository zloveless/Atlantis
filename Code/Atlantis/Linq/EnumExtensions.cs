// -----------------------------------------------------------------------------
//  <copyright file="EnumExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Linq
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	public static partial class Extensions
	{
		public static String GetDescription(this Enum source)
		{
			if (source == null) throw new ArgumentNullException("source");

			// http://weblogs.asp.net/grantbarrington/archive/2009/01/19/enumhelper-getting-a-friendly-description-from-an-enum.aspx#7801000
			return (from m in source.GetType().GetMember(source.ToString())
				let attr = (DescriptionAttribute)m.GetCustomAttributes(typeof (DescriptionAttribute), false).FirstOrDefault()
				select attr == null ? source.ToString() : attr.Description).FirstOrDefault();
		}
	}
}
