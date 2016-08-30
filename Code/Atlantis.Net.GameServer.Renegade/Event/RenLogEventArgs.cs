// -----------------------------------------------------------------------------
//  <copyright file="RenLogEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    public class RenLogEventArgs : EventArgs
    {
        public RenLogEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
