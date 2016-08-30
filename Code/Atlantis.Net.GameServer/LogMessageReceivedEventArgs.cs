// -----------------------------------------------------------------------------
//  <copyright file="LogMessageReceivedEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    public class LogMessageReceivedEventArgs : EventArgs
    {
        public LogMessageReceivedEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
