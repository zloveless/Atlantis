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

    using Atlantis.IO;

    public class RenegadeConnection : IServerConnection
    {
        #region Fields
        
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private TcpClient _client;
        private readonly ILog _logger;
        private Thread _thread;

        private readonly string _serverAddress;
        private readonly int _logPort;

        #endregion
        
        public RenegadeConnection(string serverAddress, int logPort)
        {
            _serverAddress = serverAddress;
            _logPort = logPort;

            Initialise();
        }

        public RenegadeConnection(string serverAddress, int logPort, string remoteAdminPassword, int remoteAdminPort) : this(serverAddress, logPort)
        {
            Communicator = new RenegadeCommunicator(serverAddress, remoteAdminPassword, remoteAdminPort);
        }

        public RenegadeConnection(string serverAddress, int logPort, string remoteAdminPassword, int remoteAdminPort, ILog logger) : this(serverAddress, logPort, remoteAdminPassword, remoteAdminPort)
        {
            _logger = logger;
        }

        public RenegadeConnection(string serverAddress, int logPort, ILog logger) : this(serverAddress, logPort)
        {
            _logger = logger;
        }

        #region Properties

        /// <summary>
        ///     <para>Gets a value indicating whether the current connection has been established.</para>
        /// </summary>
        public bool Connected => _client != null && _client.Connected;

        #endregion
        
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
                if (Connected)
                {
                    _tokenSource.Cancel();
                    _thread.Join();
                }
            }
        }

        #endregion

        #region Implementation of IServerConnection
        
        /// <summary>
        ///     <para>Gets a value indicating the log parser to be used for the current server connection.</para>
        /// </summary>
        public IServerParser Parser { get; private set; }

        /// <summary>
        ///     <para>Gets a value representing the communcation class in which this connection is associated.</para>
        /// </summary>
        public ServerCommunicator Communicator { get; private set; }

        /// <summary>
        ///     <para>Establishes a connection to the game server.</para>
        /// </summary>
        public void Connect()
        {
            try
            {
                _client.Connect(_serverAddress, _logPort);
            }
            catch (SocketException ex)
            {
                if (_logger != null)
                {
                    _logger.ErrorFormat("An error occurred: {0}\nSocket error code: {1}", ex.Message, (int)ex.SocketErrorCode);
                }
            }

            _thread.Start(_tokenSource.Token);
        }

        /// <summary>
        ///     <para>Closes a connection to the game server.</para>
        /// </summary>
        public void Disconnect()
        {
            _tokenSource.Cancel();
        }

        #endregion
        
        #region Methods

        private void Initialise()
        {
            _client = new TcpClient();
            _thread = new Thread(ThreadWorker);

            Parser = new RenegadeParser();
        }

        private void OnMessageReceived(string message)
        {
            // TODO: Evaluate pros/cons of trying to make events be processed on the main thread rather than the server log thread(s).
            if (Parser != null)
            {
                Parser.OnMessage(message);
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
                var stream = _client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                var sb = new StringBuilder();

                while (Connected)
                {
                    if (token != null
                        && token.Value.IsCancellationRequested)
                    {
                        _client.Close();
                        break;
                    }

                    if (!_client.Connected)
                    {
                        // TODO: Raise disconnected event and possibly reconnect too.
                        break;
                    }

                    var buffer = new char[2048];
                    var bytesRead = 0;
                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        for (var i = 0; i < bytesRead; ++i)
                        {
                            char c = buffer[i];
                            if (c == 0)
                            {
                                OnMessageReceived(sb.ToString().Trim());
                                sb.Clear();
                            }
                            else
                            {
                                sb.Append(c);
                            }
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                if (_logger != null)
                {
                    _logger.ErrorFormat("An error occured: {0}\nSocket error code: {1}", e.Message, (int)e.SocketErrorCode);
                }

                // TODO: raise client disconnection event and reconnect potentially
            }
            catch (InvalidOperationException e)
            {
                if (_logger != null)
                {
                    _logger.FatalFormat("A fatal error occurred! {0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

        #endregion
    }
}
