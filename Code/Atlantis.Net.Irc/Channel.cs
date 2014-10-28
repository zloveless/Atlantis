// -----------------------------------------------------------------------------
//  <copyright file="Target.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Collections.Generic;
	using Linq;

	public class Channel : IEquatable<String>
	{
		private readonly IrcClient client;

		private Channel()
		{
			ListModes = new List<ListMode>();
			Modes     = new Dictionary<char, string>();
			Users     = new Dictionary<string, PrefixList>();
		}

		internal Channel(IrcClient client, string channelName) : this()
		{
			this.client = client;
			Name        = channelName;
		}

		#region Properties

		public string Name { get; private set; }

		public List<ListMode> ListModes { get; private set; }

		public Dictionary<char, string> Modes { get; private set; }

		public Dictionary<string, PrefixList> Users { get; private set; }

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
