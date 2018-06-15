// -----------------------------------------------------------------------------
//  <copyright file="ModeCollection.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public class ModeCollection : IEnumerable<char>
	{
		private readonly Dictionary<char, string> modes = new Dictionary<char, string>();

		/// <summary>
		///     <para>Gets the parameter for the specified mode, if it exists.</para>
		///     <para>
		///         This method will return null if whether it exists or not if the mode has no parameter. Use
		///         <see cref="M:HasMode" /> for a reliable mode check.
		///     </para>
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public String this[char index]
		{
			get
			{
				String param = null;
				lock (modes)
				{
					if (modes.ContainsKey(index))
					{
						param = modes[index];
					}
				}

				return param;
			}
		}

		#region Implementation of IEnumerable

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<char> GetEnumerator()
		{
			return modes.Keys.GetEnumerator();
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

		#region Overrides of Object

		/// <summary>
		///     Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		///     A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return new String(modes.Keys.ToArray());
		}

		#endregion

		/// <summary>
		///     Adds the specified mode to the internal collection if it does not already exist.
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="parameter"></param>
		public void Add(char mode, String parameter = null)
		{
			lock (modes)
			{
				if (!modes.ContainsKey(mode))
				{
					modes.Add(mode, parameter);
				}
			}
		}

		/// <summary>
		///     Checks whether the internal collection already has a mode specified.
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool HasMode(char mode)
		{
			bool ret;
			lock (modes)
			{
				ret = modes.ContainsKey(mode);
			}

			return ret;
		}

		/// <summary>
		///     Removes the specified mode from the internal collection if it exists.
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool Remove(char mode)
		{
			bool ret = false;
			lock (modes)
			{
				if (modes.ContainsKey(mode))
				{
					ret = modes.Remove(mode);
				}
			}

			return ret;
		}
	}
}
