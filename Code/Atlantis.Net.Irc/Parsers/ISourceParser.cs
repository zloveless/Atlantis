// -----------------------------------------------------------------------------
//  <copyright file="ISourceParser.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Parsers
{
    public interface ISourceParser
    {
        IrcSource GetSource(string inputString);
    }
}
