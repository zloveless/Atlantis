// -----------------------------------------------------------------------------
//  <copyright file="IrcConnection.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Atlantis.Extensions;
    using Atlantis.Net.Irc.Commands;
    using Atlantis.Net.Irc.Parsers;

    public class IrcConnection
    {
        private readonly SemaphoreSlim connectingLock = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim writingLock = new SemaphoreSlim(1, 1);

        protected IDictionary<string, IRfcCommand> _commands = new ConcurrentDictionary<string, IRfcCommand>(StringComparer.OrdinalIgnoreCase);
        private TcpClient _client;
        private bool _enableSsl;
        private readonly Queue<string> _messageQueue = new Queue<string>(); 
        private readonly Thread _queueWorker;
        private StreamReader _reader;
        private NetworkStream _stream;
        private SslStream _secureStream;
        private readonly Thread _worker;

        private DateTime _lastMessage;

        public IrcConnection()
        {
            _worker = new Thread(WorkerCallback);
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the current connection is connected.
        /// </summary>
        public bool Connected
        {
            get { return _client != null && _client.Connected; }
        }

        /// <summary>
        /// Gets or sets a value determining the encoding the connection is expected to be receiving.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        ///     <para>Gets or sets a value indicating whether the current connection will be secure. Defaults to false.</para>
        ///     <para>This can only be set while the connection is in a disconnected state.</para>
        /// </summary>
        public bool EnableSsl
        {
            get { return _enableSsl; }
            set
            {
                if (!Connected)
                {
                    _enableSsl = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the host in which the connection will try to use when establishing the connection to the remote instance.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current connection has been initialized.
        /// </summary>
        public virtual bool IsInitialized
        {
            get
            {
                bool result = true;

                if (string.IsNullOrEmpty(Host)) result = false;
                else if (Port == 0) result = false;

                return result;
            }
        }

        /// <summary>
        /// Gets or sets an instance of a parser that determines modes, their parameters, and the source and target of the mode.
        /// </summary>
        public IModesStringParser ModesStringParser { get; set; }

        /// <summary>
        /// Gets or sets the port in which the connection will try to use when establishing the connection to the remote instance.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets an instance of a parser to determine the details of an RFC event.
        /// </summary>
        public ISourceParser SourceParser { get; set; }

        #endregion
        
        #region Implementation of IDisposable

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
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
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     <para>Note to implementors. This method has a pending internal TO-DO task and may/may not change visibility in the future.</para>
        ///     <para>When overriding in a derived class, the base method does not need to be called.</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="policyErrors"></param>
        /// <returns></returns>
        protected virtual bool OnPreCertificateVerify(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // TODO: Perhaps add the option (via property) to validate certificate authorities (whether they were signed by certified CA's or self-signed).
            return true;
        }

        /// <summary>
        ///     <para>Handles parsing incoming messages for numerics and RFC commands.</para>
        ///     <para>When overriding in a dervied class, please call the base method.</para>
        /// </summary>
        /// <param name="line"></param>
        protected virtual void OnLineReceived(string line)
        {
            _lastMessage = DateTime.Now;

            var tokens = line.Split(' ');
            var tokenIndex = 0;

            string source = null;
            if (tokens[tokenIndex][0] == ':')
            {
                source = tokens[tokenIndex].Substring(1);
                tokenIndex++;
            }

            if (tokenIndex == tokens.Length)
            {
                // Reached the end.
                // TODO: maybe disconnect? Idk.
                return;
            }

            var commandName = tokens[tokenIndex++];
            var parameters = new List<string>();

            while (tokenIndex != tokens.Length)
            {
                if (tokens[tokenIndex][0] != ':')
                {
                    parameters.Add(tokens[tokenIndex++]);
                    continue;
                }

                parameters.Add(string.Join(" ", tokens.Skip(tokenIndex)).Substring(1));
                break;
            }

            int numeric = 0;
            if (int.TryParse(commandName, out numeric))
            {
                OnRfcNumeric(numeric, source, parameters.ToArray());
            }
            else
            {
                OnRfcEvent(commandName, source, parameters.ToArray());
            }
        }

        protected virtual void OnRfcEvent(string commandName, string source, string[] parameters)
        {
            IRfcCommand cmd;
            if (_commands.TryGetValue(commandName, out cmd))
            {
                cmd.Execute(source, parameters);
            }
        }

        protected virtual void OnRfcNumeric(int numeric, string source, string[] parameters)
        {
            if (numeric == 1)
            {
                connectingLock.Release();
            }


        }

        /// <summary>
        ///     <para>This method is called before the main loop is initialized. Use it to send initialization commands to the server.</para>
        ///     <para>There is no need to call the base method when overriding this method.</para>
        /// </summary>
        protected virtual void OnPreConnect()
        {
        }

        public async Task<bool> Start()
        {
            if (!IsInitialized)
            {
                //throw new InvalidOperationException("The current connection has not been initialized. Please consult the documentation.");
                return false;
            }

            try
            {
                var connection = new IPEndPoint(Dns.GetHostEntry(Host).AddressList[0], Port);
                _client.Connect(connection);
            }
            catch (SocketException e)
            {
                // TODO: Socket disconnection event.
                return false;
            }

            _worker.IsBackground = true;
            _worker.Start();

            await connectingLock.WaitAsync();
            return true;
        }

        public void RegisterCommand<TCommand>(string commandName, TCommand instance) where TCommand : IRfcCommand
        {
        }

        public void Send(string format, params object[] args)
        {
            lock (_messageQueue)
            {
                _messageQueue.Enqueue(string.Format(format, args));
            }
        }

        public void SendImmediately(string format, params object[] args)
        {
        }

        private void WorkerCallback(object state)
        {
            _stream = _client.GetStream();

            OnPreConnect();

            // TODO: Accept a certificate parameter (read: properly) for sending to the IRC server.
            /*
            if (EnableSsl)
            {
                _secureStream = new SslStream(innerStream: _stream, leaveInnerStreamOpen: true, userCertificateValidationCallback: OnPreCertificateVerify);

                _reader       = new StreamReader(_secureStream, Encoding);
            }
            else
            {
                _reader = new StreamReader(_stream, Encoding);
            }
            */
            _reader = new StreamReader(_stream, Encoding);
            
            while (Connected)
            {
                if (_stream.DataAvailable)
                {
                    while (!_reader.EndOfStream)
                    {
                        string line = _reader.ReadLine().TrimIfNotNull();
                        if (!string.IsNullOrEmpty(line))
                        {
                            OnLineReceived(line);
                        }
                    }
                }
            }
        }



        #endregion
    }
}
