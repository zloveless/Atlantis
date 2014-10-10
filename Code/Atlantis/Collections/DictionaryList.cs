// -----------------------------------------------------------------------------
//  <copyright file="DictionaryList.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections
{
	using System;
	using System.Collections.Generic;

    /// <summary>
    /// Represents a dictionary that contains a list of values for any given key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class DictionaryList<T>
	{
		private readonly Dictionary<String, List<T>> dict;

		public DictionaryList()
		{
			dict = new Dictionary<string, List<T>>();
		}

		public DictionaryList(IEqualityComparer<String> comparer)
		{
			dict = new Dictionary<String, List<T>>(comparer);
		}

		public void Add(String key, T value)
		{
			List<T> values;
			if (!dict.TryGetValue(key, out values))
			{
				values = new List<T> {value};
				dict[key] = values;
			}
			else
			{
				values.Add(value);
			}
		}

		public Boolean ContainsKey(String key)
		{
			return dict.ContainsKey(key);
		}

		public Boolean Remove(String key, T value)
		{
			List<T> values;

			return dict.TryGetValue(key, out values) && values.Remove(value);
		}

		public IEnumerable<String> Keys
		{
			get { return dict.Keys; }
		}

		public Boolean TryGetValue(String key, out List<T> value)
		{
			return dict.TryGetValue(key, out value);
		}

		public List<T> this[String key]
		{
			get
			{
				List<T> values;
				if (!dict.TryGetValue(key, out values))
				{
					values = new List<T>();
					dict[key] = values;
				}

				return values;
			}
		}
	}
}
