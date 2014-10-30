// -----------------------------------------------------------------------------
//  <copyright file="Target.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using Atlantis.Linq;

	public class Channel : IEquatable<String>
	{
		private readonly IrcClient client;
		private readonly Dictionary<String, PrefixList> users = new Dictionary<String, PrefixList>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<char, String> modes = new Dictionary<char, String>();
		private readonly List<ListMode> listModes = new List<ListMode>(); 

		private Channel()
		{
		}

		internal Channel(IrcClient client, string channelName) : this()
		{
			this.client = client;
			Name        = channelName;
		}

		#region Properties

		public string Name { get; private set; }

		public IEnumerable<ListMode> ListModes
		{
			get { return listModes; }
		}
		
		public ReadOnlyDictionary<char, String> Modes
		{
			get { return new ReadOnlyDictionary<char, string>(modes); }
		}

		public ReadOnlyDictionary<String, PrefixList> Users
		{
			get { return new ReadOnlyDictionary<string, PrefixList>(users); }
		}

		#endregion

		#region Methods

		public void AddOrUpdateUser(String user, params char[] prefixes)
		{
			lock (users)
			{
				if (!users.ContainsKey(user))
				{
					users.Add(user, prefixes == null ? new PrefixList(client) : new PrefixList(client, prefixes));
				}
				else if (users.ContainsKey(user) && prefixes.Length > 0)
				{
					foreach (char p in prefixes.Where(p => !users[user].HasPrefix(p)))
					{
						users[user].AddPrefix(p);
					}
				}
			}
		}

		public bool IsUserInChannel(String user)
		{
			bool ret;
			lock (users)
			{
				ret = users.ContainsKey(user);
			}

			return ret;
		}

		public PrefixList GetUserPrefixes(String user)
		{
			PrefixList list;
			return users.TryGetValue(user, out list) ? list : null;
		}

		public void RemoveUser(String user)
		{
			lock (users)
			{
				if (users.ContainsKey(user))
				{
					users.Remove(user);
				}
			}
		}

		#endregion

		#region Overrides of Object

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(string other)
		{
			return Name.EqualsIgnoreCase(other);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return Name;
		}

		#endregion
	}
}
