// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

/**
 * https://tools.ietf.org/html/rfc1459
 * https://tools.ietf.org/html/rfc2812
 * https://tools.ietf.org/html/rfc2813
 * https://tools.ietf.org/html/rfc7194
 * https://ircv3.net/irc/
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Atlantis.Net.Irc.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlantis.Net.Irc
{
    public partial class IrcConnection
    {
        private readonly IOptions<IrcConfiguration> _config;
        private ILogger<IrcConnection> _logger;
        private Stream _networkStream;
        private TcpClient _socket;
        private Thread _thread;

        private string _host;
        private IrcRegistrationCallback _registrationCallback;

        private readonly SemaphoreSlim _writingLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _connectingLock = new SemaphoreSlim(0, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly IDictionary<string, IIrcCommand> _commands = new ConcurrentDictionary<string, IIrcCommand>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<int, IIrcNumeric> _numerics = new ConcurrentDictionary<int, IIrcNumeric>();

        public IrcConnection()
        {
        }

        public IrcConnection(IOptions<IrcConfiguration> config) : this(config, null)
        {
        }

        public IrcConnection(IOptions<IrcConfiguration> config, ILogger<IrcConnection> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        ///     <para>Gets a value indicating whether the underlying <see cref="TcpClient" /> for a <see cref="IrcClient"/> is connected to the remote host.</para>
        /// </summary>
        public bool IsConnected => _socket != null && _socket.Connected;

        /// <summary>
        ///     <para>Attempts to find the specified command handler, otherwise returns null.</para>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public TCommand FindCommand<TCommand>(string commandName) where TCommand : class, IIrcCommand
        {
            return (_commands.TryGetValue(commandName, out var value) ? value : null) as TCommand;
        }

        /// <summary>
        ///     <para>Attempts to find the specified numeric handler, otherwise returns null.</para>
        /// </summary>
        /// <typeparam name="TNumeric"></typeparam>
        /// <param name="numeric"></param>
        /// <returns></returns>
        public TNumeric FindNumeric<TNumeric>(int numeric) where TNumeric : class, IIrcNumeric
        {
            return (_numerics.TryGetValue(numeric, out var value) ? value : null) as TNumeric;
        }

        /// <summary>
        ///     <para>Registers the specified command handler with the connection.</para>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="command"></param>
        public void RegisterCommand<TCommand>(TCommand command) where TCommand : class, IIrcCommand
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (_commands.ContainsKey(command.Command))
            {
                throw new InvalidOperationException($"The specified command has already been registered: {command.Command.ToUpper()}");
            }

            _commands.Add(command.Command, command);
        }

        /// <summary>
        ///     <para>Registers the specified numeric handler with the connection.</para>
        /// </summary>
        /// <typeparam name="TNumeric"></typeparam>
        /// <param name="command"></param>
        public void RegisterNumeric<TNumeric>(TNumeric command) where TNumeric : class, IIrcNumeric
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (_numerics.ContainsKey(command.Numeric))
            {
                throw new InvalidOperationException($"The specified command has already been registered: {command.Numeric}");
            }

            _numerics.Add(command.Numeric, command);
        }

        /// <summary>
        ///     <para></para>
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <param name="portNumber"></param>
        /// <param name="registrationCallback"></param>
        /// <returns></returns>
        public async Task<bool> OpenAsync(string hostNameOrAddress, short portNumber, IrcRegistrationCallback registrationCallback)
        {
            try
            {
                _socket = new TcpClient();
                _thread = new Thread(ThreadCallback)
                {
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = true
                };

                await _socket.ConnectAsync(hostNameOrAddress, portNumber);

                _host = hostNameOrAddress;

                var cancellationToken = _cts.Token;
                _thread.Start(cancellationToken);
            }
            catch (SocketException ex)
            {
                _logger?.LogError(ex, $"An error occurred while attempting to connect to the remote host: {ex.Message}\n\nSocket Error Code: {(int)ex.SocketErrorCode}");
            }

            _registrationCallback = registrationCallback;

            await _connectingLock.WaitAsync();
            return IsConnected && _connectingLock.CurrentCount == 1;
        }

        public void Close()
        {
            _cts.CancelAfter(millisecondsDelay: 100);
        }

        private void ThreadCallback(object arg0)
        {
            if (_config.Value.EnableSsl)
            {
                var baseStream = _socket.GetStream();
                var sslStream = new SslStream(baseStream, leaveInnerStreamOpen: false, userCertificateValidationCallback: ValidationCallback);
                var cert = GetCertificate(_config.Value.SslCertificate, _config.Value.SslCertificateKey);

                sslStream.AuthenticateAsClient(_host, new X509CertificateCollection(new[] { cert }), System.Security.Authentication.SslProtocols.Tls12, false);
            }
            else
            {
                _networkStream = _socket.GetStream();
            }

            using (var reader = new StreamReader(_networkStream))
            {
                while (IsConnected)
                {
                    if (_cts.IsCancellationRequested) break;

                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;

                    OnDataReceived(line);
                }
            }
        }

        private bool ValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            // TODO: Do certificate subject and domain check?
            return true;
        }

        protected virtual void OnDataReceived(string message)
        {
            if (_registrationCallback != null)
            {
                _registrationCallback(this);
                _registrationCallback = null;
            }

            // TODO!
        }

        private X509Certificate GetCertificate(string clientCertificate, string certificatePrivateKeyFile)
        {
            return null;
        }
    }
}
