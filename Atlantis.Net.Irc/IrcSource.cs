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

        private string _source;
        
        /// <summary>
        ///     <para>Represents the source username, if it is a user.</para>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     <para>Represents the source ident mask, if it is a user.</para>
        /// </summary>
        public string Ident { get; private set; }

        /// <summary>
        ///     <para>Represents the source hostname, if it is a user.</para>
        /// </summary>
        public string Host { get; private set; }

        public override string ToString()
        {
            return _source;
        }

        public static IrcSource? Parse(string source)
        {
            var ret = new IrcSource();

            ret._source = source;
            ret.Name  = "";
            ret.Ident = "";
            ret.Host  = "";

            Match m;
            if (!(m = SourceRegex.Match(source)).Success)
            {
                return null;
            }

            var userOrServer = m.Groups[1].Value; // user or server
            if (m.Groups[2].Success)
            {
                ret.Ident = m.Groups[2].Value;
                ret.Host  = m.Groups[3].Value;
            }

            ret.Name = userOrServer;
            return ret;
        }

        public static bool TryParse(string source, out IrcSource? value)
        {
            if (string.IsNullOrEmpty(source) || !SourceRegex.IsMatch(source))
            {
                value = null;
                return false;
            }

            var result = Parse(source);

            value = result;
            return result != null;
        }
    }
}
