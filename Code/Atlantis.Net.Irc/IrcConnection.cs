// -----------------------------------------------------------------------------
//  <copyright file="IrcConnection.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Atlantis.Collections;
    using Atlantis.Extensions;

    /// <summary>
    ///     <para>Represents a TCP connection that uses the IRC protocol.</para>
    /// </summary>
    public class IrcConnection
    {
        private readonly SemaphoreSlim _connectingLock = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _writingLock    = new SemaphoreSlim(0, 1);

        private TcpClient _client;
        private Stream _stream;
        private StreamReader _reader;
        private Thread _workerThread;

        private readonly Queue<string> _messageQueue = new Queue<string>();
        private Thread _queueWorker;

        private readonly GenericDataContainer _data = new GenericDataContainer();
        private Encoding _encoding;
        private IrcProtocol _protocol;

        public IrcConnection()
        {
            _workerThread = new Thread(WorkerThreadCallback);
            _queueWorker = new Thread(QueueWorkerCallback);
        }

        public IrcConnection(IrcProtocol protocol) : this()
        {
            Protocol = protocol;
        }

        /// <summary>
        ///     <para>Gets a value indicating whether the connection is active and connected to the host.</para>
        /// </summary>
        public bool Connected => _client != null && _client.Connected;

        /// <summary>
        ///     <para>Gets or sets a value indicating the encoding to be used when performing I/O operatings on the connection.</para>
        /// </summary>
        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                if (!Connected)
                {
                    _encoding = value;
                }
            }
        }

        /// <summary>
        ///     <para>Sets a value indicating the protocol to be used while the connection is active.</para>
        /// </summary>
        public IrcProtocol Protocol
        {
            private get { return _protocol; }
            set
            {
                if (!Connected)
                {
                    _protocol = value;
                }
            }
        }

        public void Send(string message, params object[] args)
        {
            lock (_messageQueue)
            {
                _messageQueue.Enqueue(string.Format(message, args));
            }
        }

        /// <summary>
        ///     <para>Sends the specified formatted message to the connection after the specified time.</para>
        /// </summary>
        /// <param name="delay">The time to delay sending the message in <see cref="T:System.TimeSpan" /> format.</param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task Send(TimeSpan delay, string message, params object[] args)
        {
            return Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(delay);
                    return SendImmediately(string.Format(message, args));
                });
        }

        /// <summary>
        ///     <para>Sends the specified formatted message to the connection after the specified time.</para>
        /// </summary>
        /// <param name="delay">The time to delay sending the message in milliseconds.</param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task Send(int delay, string message, params object[] args)
        {
            return Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(delay);
                    return SendImmediately(string.Format(message, args));
                });
        }

        private async Task<bool> SendImmediately(string message)
        {
            if (!Connected)
            {
                return false;
            }

            await _writingLock.WaitAsync();

            try
            {
                var dataMessage = new StringBuilder(message);
                dataMessage.Append('\n');

                byte[] buf = Encoding.GetBytes(dataMessage.ToString());

                await _stream.WriteAsync(buf, 0, buf.Length);
                await _stream.FlushAsync();

                return true;
            }
            finally
            {
                _writingLock.Release();
            }
        }

        private void WorkerThreadCallback(object state)
        {
            if (state != null)
            {
                
            }

            Stream stream = _client.GetStream();
            
            Protocol.RfcRegister();

            // Handle SSL stuff
            //  * Reset "stream" to be an SslStream if enabled.

            _stream = stream;
            _reader = new StreamReader(_stream, Encoding);

            while (Connected)
            {
                if (_client.Available != 0)
                {
                    while (!_reader.EndOfStream)
                    {
                        string line = _reader.ReadLine().TrimIfNotNull();
                        if (!string.IsNullOrEmpty(line))
                        {
                            // Send off to handler.
                            Protocol.OnMessageReceived(line);
                        }
                    }
                }
            }
        }

        private void QueueWorkerCallback(object state)
        {
        }
    }
}
