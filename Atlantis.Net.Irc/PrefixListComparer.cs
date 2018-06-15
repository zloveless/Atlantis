// -----------------------------------------------------------------------------
//  <copyright file="PrefixListComparer.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System.Collections.Generic;

    public class PrefixListComparer : IComparer<PrefixList>
    {
        private readonly IrcClient _client;

        public PrefixListComparer(IrcClient client)
        {
            _client = client;
        }

        #region Implementation of IComparer<in PrefixList>

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <returns>
        ///     A signed integer that indicates the relative values of <paramref name="a" /> and <paramref name="b" />, as shown in
        ///     the following table.Value Meaning Less than zero<paramref name="a" /> is less than <paramref name="b" />.Zero
        ///     <paramref name="a" /> equals <paramref name="b" />.Greater than zero<paramref name="a" /> is greater than
        ///     <paramref name="b" />.
        /// </returns>
        /// <param name="a">The first object to compare.</param>
        /// <param name="b">The second object to compare.</param>
        public int Compare(PrefixList a, PrefixList b)
        {
            return new PrefixComparer(_client).Compare(a.HighestPrefix, b.HighestPrefix);
        }

        #endregion
    }
}
