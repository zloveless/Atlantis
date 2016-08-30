// -----------------------------------------------------------------------------
//  <copyright file="SSGMLogEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    public class SsgmLogEventArgs : RenLogEventArgs
    {
        public SsgmLogEventArgs(string message, string header) : base(message)
        {
            Header = header;
        }

        public string Header { get; private set; }
    }
}
