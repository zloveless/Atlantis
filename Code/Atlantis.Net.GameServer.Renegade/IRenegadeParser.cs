// -----------------------------------------------------------------------------
//  <copyright file="IRenegadeParser.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    public interface IRenegadeParser
    {
        event EventHandler<SsgmLogEventArgs> SsgmLog;
        event EventHandler<RenLogEventArgs> RenegadeLog;
        event EventHandler<RenLogEventArgs> GameLog;
        event EventHandler<RenLogEventArgs> ConsoleLog;
    }
}
