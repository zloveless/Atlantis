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
 * 
 * ---
 * https://stackoverflow.com/a/2047657
 * http://blog.ploeh.dk/2014/05/19/di-friendly-library/
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
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
        private readonly short _connectionType;
        private readonly ILogger<IrcConnection> _logger;

        private Stream _networkStream;
        private TcpClient _socket;
        private Thread _thread;
        
        private string _host;
        private string _currentNick;
        private Encoding _encoding = new UTF8Encoding();

        private IrcRegistrationCallback _registrationCallback;

        private readonly SemaphoreSlim _writingLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _connectingLock = new SemaphoreSlim(0, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly IDictionary<string, IIrcCommand> _commands = new ConcurrentDictionary<string, IIrcCommand>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<int, IIrcNumeric> _numerics = new ConcurrentDictionary<int, IIrcNumeric>();

        public const short IrcConnectionTypeClient = 1;
        public const short IrcConnectionTypeServer = 2;

        public IrcConnection(short connectionType, IOptions<IrcConfiguration> config) : this(connectionType, config, null)
        {
        }

        public IrcConnection(short connectionType, IOptions<IrcConfiguration> config, ILogger<IrcConnection> logger)
        {
            _config = config;
            _logger = logger;

            _connectionType = connectionType;
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

        public async Task SendAsync(string message)
        {
            try
            {
                await _writingLock.WaitAsync();

                var msg = new StringBuilder(message).Append('\n');
                var buf = _encoding.GetBytes(msg.ToString());

                await _networkStream.WriteAsync(buf, 0, buf.Length);
                await _networkStream.FlushAsync();
            }
            finally
            {
                _writingLock.Release();
            }
        }

        public Task SendAsync(string messageFormat, params object[] args)
        {
            var msg = new StringBuilder().AppendFormat(messageFormat, args);
            return SendAsync(msg.ToString());
        }

        public async Task SetNickAsync(string nickname)
        {
            // Don't bother trying to send/flood the server with NICK commands.
            if (nickname.Equals(_currentNick, StringComparison.OrdinalIgnoreCase)) return;

            await SendAsync($"NICK {nickname}").ContinueWith(x =>
            {
                _currentNick = nickname;
            });
        }
    }
}
