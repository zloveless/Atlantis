// -----------------------------------------------------------------------------
//  <copyright file="IRenegadeEvents.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    public interface IRenegadeEvents
    {
        event EventHandler<SsgmLogEventArgs> SsgmLog;
        event EventHandler<RenLogEventArgs> RenegadeLog;
        event EventHandler<GameLogEventArgs> GameLog;
        event EventHandler<RenLogEventArgs> ConsoleLog;
        event EventHandler<RenLogEventArgs> InvalidLog;
    }
}
