// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Atlantis.Net.Irc
{
    public struct IrcSource
    {
        private static readonly Regex SourceRegex = new Regex(@"^\:?([^! ]+)(?:\!([^@]+)\@([^ ]+))?", RegexOptions.IgnoreCase);

        private readonly string _source;

        public IrcSource(string source)
        {
            _source = source;

            Name  = "";
            Ident = "";
            Host  = "";

            Match m;
            if (!(m = SourceRegex.Match(source)).Success) return;

            var userOrServer = m.Groups[1].Value; // user or server
            if (m.Groups[2].Success)
            {
                Ident = m.Groups[2].Value;
                Host = m.Groups[3].Value;
            }

            Name = userOrServer;
        }

        /// <summary>
        ///     <para>Represents the source username, if it is a user.</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     <para>Represents the source ident mask, if it is a user.</para>
        /// </summary>
        public string Ident { get; }

        /// <summary>
        ///     <para>Represents the source hostname, if it is a user.</para>
        /// </summary>
        public string Host { get; }

        public override string ToString()
        {
            return _source;
        }
    }
}
