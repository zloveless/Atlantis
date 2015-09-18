// -----------------------------------------------------------------------------
//  <copyright file="IrcClientSourceParser.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Parsers
{
    using System.Text.RegularExpressions;

    public class IrcClientSourceParser : ISourceParser
    {
        private static readonly Regex clientSourceRegex = new Regex(@"^:?(?<nick>[^!]+)\!(?<ident>[^@]+)\@(?<hostname>[^ ]+)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex serverSourceRegex = new Regex(@"^:?(?<server>[^ ]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        #region Implementation of ISourceParser

        public IrcSource GetSource(string inputString)
        {
            var clientMatch = clientSourceRegex.Match(inputString);
            var serverMatch = serverSourceRegex.Match(inputString);
            if (clientMatch.Success
                && !serverMatch.Success)
            {
                var nick = clientMatch.Groups["nick"].Value;
                var ident = clientMatch.Groups["ident"].Value;
                var hostname = clientMatch.Groups["hostname"].Value;

                return new IrcSource(nick, ident, hostname);
            }

            if (serverMatch.Success)
            {
                var serverName = serverMatch.Groups["server"].Value;

                return new IrcSource(serverName);
            }

            return default(IrcSource);
        }

        #endregion
    }
}
