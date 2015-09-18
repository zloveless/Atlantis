// -----------------------------------------------------------------------------
//  <copyright file="Prefixes.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//      
//      LICENSE TBA
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Linq;

    public struct PrefixList : IComparable<PrefixList>, IComparable<char>
	{
		private readonly IrcClient _client;
		private readonly char[] prefixes;

		public PrefixList(IrcClient client)
		{
			_client = client;
            prefixes = new char[client.ServerInfo.Prefixes.Length];
		}

		public PrefixList(IrcClient client, char[] prefixList) : this(client)
		{
			prefixes = prefixList;

            if (prefixes.Length != client.ServerInfo.Prefixes.Length)
			{
                Array.Resize(ref prefixes, client.ServerInfo.Prefixes.Length);
				Resort();
			}
		}

		public char HighestPrefix
		{
			get { return prefixes.Length > 0 ? prefixes[0] : (char)0; }
		}

	    #region Methods

	    public bool AddPrefix(char prefix)
	    {
	        for (var i = 0; i < prefixes.Length; ++i)
	        {
	            if (prefixes[i] == 0 || prefixes[i] == prefix)
	            {
	                var success = prefixes[i] == 0;
	                prefixes[i] = prefix;

	                if (success)
	                {
	                    Resort();
	                }

	                return success;
	            }
	        }

	        return false;
	    }

	    public bool HasPrefix(char prefix)
	    {
	        return prefixes.Any(t => t == prefix);
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

        private void Resort()
	    {
	        Array.Sort(prefixes, Sort);
	    }

        private int Sort(char a, char b)
	    {
	        if (a == 0 && b == 0) return 0;
	        if (a == 0) return 1;
	        if (b == 0) return -1;

	        var aIndex = _client.ServerInfo.Prefixes.IndexOf(a);
            var bIndex = _client.ServerInfo.Prefixes.IndexOf(b);

	        if (aIndex < 0 || bIndex < 0)
	        {
	            return 0;
	        }

	        return aIndex - bIndex;
	    }

	    #endregion

	    #region Implementation of IComparable<in PrefixList>

	    /// <summary>
	    /// Compares the current object with another object of the same type.
	    /// </summary>
	    /// <returns>
	    /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
	    /// </returns>
	    /// <param name="other">An object to compare with this object.</param>
	    public int CompareTo(PrefixList other)
	    {
	        return new PrefixListComparer(_client).Compare(this, other);
	    }

	    #endregion
        
	    #region Implementation of IComparable<in char>

	    /// <summary>
	    /// Compares the current object with another object of the same type.
	    /// </summary>
	    /// <returns>
	    /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
	    /// </returns>
	    /// <param name="other">An object to compare with this object.</param>
	    public int CompareTo(char other)
	    {
	        return new PrefixComparer(_client).Compare(HighestPrefix, other);
	    }

	    #endregion
        
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
