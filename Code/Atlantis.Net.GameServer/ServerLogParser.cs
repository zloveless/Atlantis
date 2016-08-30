// -----------------------------------------------------------------------------
//  <copyright file="ServerLogParser.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    public abstract class ServerLogParser
    {
        private readonly IServerConnection _serverConnection;

        protected ServerLogParser(IServerConnection serverConnection)
        {
            _serverConnection = serverConnection;

            if (_serverConnection != null)
            {
                _serverConnection.MessageReceived += OnLog;
            }
        }

        protected abstract void OnLog(object sender, LogMessageReceivedEventArgs args);
    }
}
