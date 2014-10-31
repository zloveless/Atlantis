// -----------------------------------------------------------------------------
//  <copyright file="ListModeCollection.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public class ListModeCollection : IEnumerable<ListMode>
	{
		private readonly List<ListMode> modes = new List<ListMode>();

		/// <summary>
		///     Returns a list of stored for the specified list index.
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public String[] this[char mode]
		{
			get { return modes.Where(x => x.Mode == mode).Select(x => x.Mask).ToArray(); }
		}

		#region Implementation of IEnumerable

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<ListMode> GetEnumerator()
		{
			return modes.GetEnumerator();
		}

		/// <summary>
		///     Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public void Add(char mode, String mask, String setter, DateTime? date = null)
		{
			lock (modes)
			{
				if (!modes.Any(x => x.Mode == mode && x.Mask.Equals(mask, StringComparison.OrdinalIgnoreCase)))
				{
					modes.Add(new ListMode(mode, mask, setter, date.HasValue ? date.Value : DateTime.UtcNow));
				}
			}
		}

		public bool Remove(char mode, String mask)
		{
			bool ret = false;
			lock (modes)
			{
				var m = modes.SingleOrDefault(x => x.Mode == mode && x.Mask.Equals(mask, StringComparison.OrdinalIgnoreCase));
				if (m != null)
				{
					ret = modes.Remove(m);
				}
			}

			return ret;
		}
	}
}
