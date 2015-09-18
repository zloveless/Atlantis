// -----------------------------------------------------------------------------
//  <copyright file="IModesStringParser.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Parsers
{
    using System.Collections.Generic;

    public interface IModesStringParser
    {
        IEnumerable<GenericMode> GetModeList(string modeString, params string[] parameters);
    }
}
