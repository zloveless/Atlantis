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
		
		private Channel()
		{
			Modes = new ModeCollection();
			ListModes = new ListModeCollection();
		}

		internal Channel(IrcClient client, string channelName) : this()
		{
			this.client = client;
			Name        = channelName;
		}

		#region Properties

		public string Name { get; private set; }

		public ListModeCollection ListModes { get; private set; }

		public ModeCollection Modes { get; private set; }

		public ReadOnlyDictionary<String, PrefixList> Users
		{
			get { return new ReadOnlyDictionary<String, PrefixList>(users); }
		}

		#endregion

		#region Methods

		public void AddOrUpdateUser(String user, params char[] prefixes)
		{
			lock (users)
			{
				if (!users.ContainsKey(user))
				{
					users.Add(user, prefixes.Length == 0 ? new PrefixList(client) : new PrefixList(client, prefixes));
				}
				else
				{
					foreach (char p in prefixes.Where(x => !users[user].HasPrefix(x)))
					{
						users[user].AddPrefix(p);
					}
				}
			}
		}

		public void AddPrefix(String username, char prefix)
		{
			lock (users)
			{
				PrefixList l;
				if (!users.TryGetValue(username, out l))
				{
					l = new PrefixList(client);
					users.Add(username, l);
				}

				if (!l.HasPrefix(prefix))
				{
					l.AddPrefix(prefix);
				}
			}
		}

		public String GetUserPrefixes(String user)
		{
			PrefixList list;
			return users.TryGetValue(user, out list) ? list.ToString() : null;
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

		public void RemovePrefix(String username, char prefix)
		{
			lock (users)
			{
				PrefixList l;
				if (!users.TryGetValue(username, out l))
				{
					l = new PrefixList(client);
					users.Add(username, l);
				}

				if (l.HasPrefix(prefix))
				{
					l.RemovePrefix(prefix);
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
