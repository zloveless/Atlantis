// -----------------------------------------------------------------------------
//  <copyright file="PrefixList.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//      
//      LICENSE TBA
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class PrefixList
	{
		private readonly IrcClient_Old clientOld;
		private readonly char[] prefixes;

		public PrefixList(IrcClient_Old clientOld)
		{
			this.clientOld = clientOld;
			prefixes = new char[clientOld.AccessPrefixes.Length];
		}

		public char HighestPrefix
		{
			get { return prefixes.Length > 0 ? prefixes[0] : (char)0; }
		}

		public bool AddPrefix(char prefix)
		{
			for (var i = 0; i < prefixes.Length; ++i)
			{
				if (prefixes[i] == 0 || prefixes[i] == prefix)
				{
					var success = prefixes[i] == 0;
					prefixes[i] = prefix;

					if (success) Resort();

					return success;
				}
			}

			return false;
		}

		public bool RemovePrefix(char prefix)
		{
			for (var i = 0; i < prefixes.Length; ++i)
			{
				if (prefixes[i] == prefix)
				{
					prefixes[i] = (char)0;
					Resort();

					return true;
				}
			}

			return false;
		}

		protected void Resort()
		{
			Array.Sort(prefixes, Sort);
		}

		protected int Sort(char a, char b)
		{
			if (a == 0 && b == 0) return 0;
			if (a == 0) return 1;
			if (b == 0) return -1;

			var aIndex = clientOld.AccessPrefixes.IndexOf(a);
			var bIndex = clientOld.AccessPrefixes.IndexOf(b);

			if (aIndex < 0 || bIndex < 0)
			{
				return 0;
			}

			return aIndex - bIndex;
		}

		#region Overrides of Object

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return new string(prefixes);
		}

		#endregion
	}
}
