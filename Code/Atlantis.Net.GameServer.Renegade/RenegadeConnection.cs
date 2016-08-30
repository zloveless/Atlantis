// -----------------------------------------------------------------------------
//  <copyright file="RenegadeConnection.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------
namespace Atlantis.Net.GameServer
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Atlantis.Extensions;

    public class RenegadeConnection : IServerConnection
    {
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private TcpClient _client;
        //private RenRem _rencomm; // TODO: Replace with a generic implementation and remove this hard dependency
        private Thread _thread;

        private string _serverAddress;
        private int _logPort;

        public RenegadeConnection(string serverAddress, int logPort, string remoteAdminPass = "", int remoteAdminPort = 1111)
        {
            _client = new TcpClient();
            _thread = new Thread(ThreadWorker);

            _serverAddress = serverAddress;
            _logPort = logPort;

            // TODO: Resolve an IServerCommunicator using remoteAdminPass and remoteAdminPort
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                
            }
        }

        #endregion

        #region Implementation of IServerConnection

        /// <summary>
        ///     <para>Occurs when a log message was received by the game server.</para>
        /// </summary>
        public event EventHandler<LogMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     <para>Gets a value representing the communication method for the connection.</para>
        /// </summary>
        public IServerCommunicator Communicator { get; private set; }

        /// <summary>
        ///     <para>Establishes a connection to the game server.</para>
        /// </summary>
        public void Connect()
        {
        }

        /// <summary>
        ///     <para>Closes a connection to the game server.</para>
        /// </summary>
        public void Disconnect()
        {
        }

        #endregion

        /// <summary>
        ///     <para>Gets a value indicating whether the current connection has been established.</para>
        /// </summary>
        public bool Connected => _client != null && _client.Connected;

        private void OnMessageReceived(string message)
        {
            // TODO: Evaluate pros/cons of trying to make events be processed on the main thread rather than the server log thread(s).

            // ReSharper disable once UseNullPropagation
            if (MessageReceived != null)
            {
                MessageReceived.Raise(this, new LogMessageReceivedEventArgs(message));
            }
        }

        private void ThreadWorker(object arg0)
        {
            CancellationToken? token = null;
            if (arg0 is CancellationToken)
            {
                token = (CancellationToken)arg0;
            }

            try
            {
                var stream  = _client.GetStream();
                var reader  = new StreamReader(stream, Encoding.UTF8);
                var sb      = new StringBuilder();

                while (Connected)
                {
                    if (token != null
                        && token.Value.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!_client.Connected)
                    {
                        // TODO: Raise disconnected event and possibly reconnect too.
                        break;
                    }

                    int c;
                    while ((c = reader.Read()) >= 0)
                    {
                        if (c == 0)
                        {
                            OnMessageReceived(sb.ToString().Trim());
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append((char)c);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                // TODO: Logger plz.
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("An error occurred: {0}", e.Message);
                Console.WriteLine("Error code: {0:0.0}", (int)e.SocketErrorCode);
                Console.ForegroundColor = c;

                // TODO: raise client disconnection event and reconnect potentially
            }
        }
    }
}
