using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Atlantis.Net.Irc.Events;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Atlantis.Net.Irc;

[PublicAPI]
public class IrcClient
{
    public const string Version = "Atlantis.Net.Irc/5.0.0 (.NET 9.0) - Source Code: https://github.com/zloveless/Atlantis";
    
    /// <summary>
    /// Returns a set of capabilities that the <see cref="IrcClient" /> supports and expects.
    /// </summary>
    private static readonly List<string> RequestedCapabilities =
    [
        IrcV3Capabilities.MessageTags,
        IrcV3Capabilities.MultiPrefix,
        IrcV3Capabilities.UserHostInNames
    ];
    
    private string _channelTypes;
    private ChannelModes _chanModes;
    private Regex _multiPrefixNames;
    private string _prefixSymbols;
    private string _prefixModes;
    
    private readonly IrcClientConfiguration _config;
    private readonly ILogger? _logger;
    private IrcConnection _connection;

    private readonly Dictionary<string, Channel> _channels = new(StringComparer.OrdinalIgnoreCase);
    
    private readonly List<string> _enabledCapabilities = [];
    private readonly SemaphoreSlim _registrationLock = new(0, 1);
    
    private int _lastNumeric = -1;
    private bool _serverFeatureEventFired;
    
    private readonly StringBuilder _motd = new();
    private Dictionary<string, string> _serverFeatureSupport = new(StringComparer.OrdinalIgnoreCase);
    
    public IrcClient(IrcClientConfiguration config, ILogger? logger = null)
    {
        _config = config;
        _logger = logger;
        _connection = new IrcConnection(OnConnect, OnDataReceived, stream => stream.AuthenticateAsClient(HostName));
    }

    #region Properties
    
    /// <inheritdoc cref="IrcConnection.HostName" />
    public string HostName
    {
        get => _connection.HostName;
        set => _connection.HostName = value;
    }
    
    /// <inheritdoc cref="IrcConnection.Port" />
    public short Port
    {
        get => _connection.Port;
        set => _connection.Port = value;
    }
    
    /// <inheritdoc cref="IrcConnection.UseSsl" />
    public bool UseSsl
    {
        get => _connection.UseSsl;
        set => _connection.UseSsl = value;
    }

    internal bool UseMultiPrefix => SupportsCapability(IrcV3Capabilities.MultiPrefix) && _multiPrefixNames != null;

    #endregion

    #region Events

    /// <summary>
    /// Raised when CAP negotiation responds with an ACK message, confirming the requested capabilities.
    /// </summary>
    public event EventHandler<CapAckReceivedEventArgs> CapAckReceivedEvent; 

    /// <summary>
    /// Event fired when an IRC client connection receives numeric RPL_WELCOME (001).
    /// </summary>
    public event EventHandler ConnectionEstablishedEvent;

    /// <summary>
    /// Raised when the client receives a CTCP event.
    /// </summary>
    public event EventHandler<CtcpReceivedEventArgs> CtcpReceivedEvent; 

    /// <summary>
    /// Event fired when the IRC connection receives an ERROR command.
    /// </summary>
    public event EventHandler<IrcErrorEventArgs> ErrorReceivedEvent;
    
    /// <summary>
    /// Raised when the client notices a PRIVMSG to a channel.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs> ChannelMessageReceivedEvent;
        
    /// <summary>
    /// Raised when the client receives a notice from the server to which its connected. 
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs> ServerNoticeReceivedEvent;
    
    /// <summary>
    /// Raised when the client receives a PRIVMSG from another user.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs> PrivateMessageReceivedEvent; 

    /// <summary>
    /// Event fired at the end of the MOTD transmission.
    /// </summary>
    public event EventHandler<MotdEventArgs> MotdReceivedEvent;

    /// <summary>
    /// Raised when a user joins a channel being monitored by the IrcClient.
    /// </summary>
    public event EventHandler<JoinPartEventArgs> JoinEvent;
    
    /// <summary>
    /// Raised when a user leaves a channel being monitored by the IrcClient.
    /// </summary>
    public event EventHandler<JoinPartEventArgs> PartEvent;

    /// <summary>
    /// Raised when a user is forcefully removed from a channel.
    /// </summary>
    public event EventHandler<KickEventArgs> KickEvent; 

    /// <summary>
    /// Event fired after the last RPL_ISUPPORT (005) line received. Multiple lines buffered into a single event fire.
    /// </summary>
    public event EventHandler<ServerFeaturesReceivedEventArgs> ServerFeaturesReceivedEvent;

    /// <summary>
    /// Raised when a channel's topic was changed. 
    /// </summary>
    public event EventHandler<TopicChangedEventArgs> TopicChangedEvent; 

    #endregion

    #region Methods

    /// <summary>
    /// Adds a new or updates an existing channel mode to the internal registrar.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="channelMode"></param>
    /// <param name="remove">Whether or not to remove the channel mode.</param>
    private void AddOrUpdateModeOnChannel(string channelName, ChannelMode channelMode, bool remove = false)
    {
        if (!_channels.TryGetValue(channelName, out var channel)) return;

        // Checks if the parameter for this mode is required and whether it's set.
        bool IsModeParameterRequired(ChannelMode cm) => cm.Type == ModeType.NoParam && cm.Parameter == null;

        // Checks whether the parameter is NOT null and if it matches the provided parameter.
        bool DoesRequiredParameterMatchProvidedParam(ChannelMode cm) => cm.Parameter != null &&
                                                                        cm.Parameter.Equals(channelMode.Parameter,
                                                                            StringComparison.OrdinalIgnoreCase);
            
        // Checks if the parameter is required and whether it matches the provided channelMode 
        bool DoesParameterMatch(ChannelMode cm) =>
            IsModeParameterRequired(cm) || DoesRequiredParameterMatchProvidedParam(cm);

        var current =
            channel.Modes.FirstOrDefault(cm => cm.Mode.Equals(channelMode.Mode) && DoesParameterMatch(cm));
            
        if (current != null)
        {
            channel.Modes.Remove(current);
        }
            
        if (!remove)
        {
            channel.Modes.Add(channelMode);
        }
    }
            
    /// <summary>
    /// Returns a value whether or not the specified target is a channel name or not.
    /// </summary>
    /// <param name="target">The name to check whether its a channel.</param>
    /// <returns>Whether the specified target is a channel according to the received prefixes.</returns>
    private bool IsChannelName(string target)
    {
        return !string.IsNullOrEmpty(_channelTypes) && _channelTypes.Any(target.StartsWith);
    }

    /// <summary>
    /// Gets a user's channel modes for the specified channel.
    /// </summary>
    /// <param name="channelName">The channel to look up a user's access level.</param>
    /// <param name="userPrefix">The requested user's full prefix when looking up their access.</param>
    /// <exception cref="ArgumentNullException">Thrown when either of the two arguments are invalid values such as null or empty strings.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified channel does not exist in the <see cref="IrcClient" />'s internal channel registrar.</exception>
    /// <returns>The user's mode prefixes</returns>
    public string GetChannelUserModes(string channelName, string userPrefix) 
    {
        if (string.IsNullOrEmpty(channelName))
        {
            throw new ArgumentNullException(nameof(channelName));
        }
        
        if (string.IsNullOrEmpty(userPrefix))
        {
            throw new ArgumentNullException(nameof(userPrefix));
        }
        
        if (!_channels.ContainsKey(channelName))
        {
            throw new ArgumentOutOfRangeException(nameof(channelName));
        }

        if (!_channels.TryGetValue(channelName, out var channel)) return string.Empty;
        return channel.TryGetUserModes(userPrefix, out var channelUser) ? channelUser.Modes : string.Empty;
    }

    /// <summary>
    /// Returns a <see cref="ChannelUser" /> if they exist on the channel.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private ChannelUser? FindUserInChannel(string channelName, string userName) 
    {
        if (string.IsNullOrEmpty(channelName))
        {
            throw new ArgumentNullException(nameof(channelName));
        }
        
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }
        
        return _channels.TryGetValue(channelName, out var channel) ? channel.FindUser(userName) : null;
    }

    /// <summary>
    /// Gets a Channel reference from the specified key.
    /// </summary>
    /// <param name="channelName">The channel name to lookup.</param>
    /// <param name="value">When this method returns true if a channel is found, contains the Channel reference for the specified channel key.</param>
    /// <param name="createNew">Creates the channel if true and it doesn't exist.</param>
    /// <returns>true if the <see cref="IrcClient"/> contains the channel reference, otherwise false</returns>
    public bool TryGetChannel(string channelName, [MaybeNullWhen(false)] out Channel value, bool createNew = false)
    {
        var exists = _channels.TryGetValue(channelName, out value);
        if (exists || !createNew) return exists;
        exists = createNew;

        var result = AddChannel(channelName);
        value = result;
        return exists;
    }

    /// <summary>
    /// Returns an enumerable of channel modes from mode strings and parameters received from a MODE command.
    /// </summary>
    /// <param name="modes">A complete mode string containing alpha-characters and +/- symbols, indicating setting modes on a channel.</param>
    /// <param name="parameters">The parameters for all the modes received in the event.</param>
    /// <returns>An enumerable of modes, allowing processing as modes are returned from the method.</returns>
    protected IEnumerable<GenericMode> ParseChannelModes(string modes, params string[] parameters) 
    {
        var set = false;
        for (int modeIndex = 0, parameterIndex = 0; modeIndex < modes.Length; ++modeIndex)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (modes[modeIndex] == '+') set = true;
            else if (modes[modeIndex] == '-') set = false;
            else if (_chanModes.ListModes.Contains(modes[modeIndex]))
            {
                var arg = parameters[parameterIndex];
                parameterIndex++;
                yield return new GenericMode(modes[modeIndex], arg, set, ModeType.List);
            }
            else if (_chanModes.ModesWithParameter.Contains(modes[modeIndex]))
            {
                var arg = parameters[parameterIndex];
                parameterIndex++;
                yield return new GenericMode(modes[modeIndex], arg, set, ModeType.SetUnset);
            }
            else if (_chanModes.ModesWithParametersWhenSet.Contains(modes[modeIndex]))
            {
                var arg = string.Empty;
                if (set)
                {
                    arg = parameters[parameterIndex];
                    parameterIndex++;
                }

                yield return new GenericMode(modes[modeIndex], arg, set, ModeType.Set);
            }
            else if (_chanModes.ModesWithNoParameter.Contains(modes[modeIndex]))
            {
                yield return new GenericMode(modes[modeIndex], string.Empty, set, ModeType.NoParam);
            }
            else if (_prefixModes.Contains(modes[modeIndex]))
            {
                var arg = parameters[parameterIndex];
                parameterIndex++;
                yield return new GenericMode(modes[modeIndex], arg, set, ModeType.Access);
            }
        }
    }

    /// <summary>
    /// Adds and returns the specified channel to the internal channel registry
    /// </summary>
    /// <param name="channelName"></param>
    /// <returns></returns>
    protected Channel AddChannel(string channelName)
    {
        if (!IsChannelName(channelName))
        {
            throw new ArgumentException($"The specified 'channel' is invalid: {channelName}", nameof(channelName));
        }
        
        if (_channels.TryGetValue(channelName, out var channel)) return channel;
        
        var result = new Channel(channelName, this);
        _channels.Add(channelName, result);
        return result;
    }

    /// <summary>
    /// Removes the specified channel from being tracked by the client.
    /// </summary>
    /// <param name="channelName"></param>
    /// <exception cref="ArgumentException"></exception>
    protected void RemoveChannel(string channelName) 
    {
        if (!IsChannelName(channelName))
        {
            throw new ArgumentException($"The specified 'channel' is invalid: {channelName}", nameof(channelName));
        }
        
        _channels.Remove(channelName);
    }
    
    /// <summary>
    /// Returns whether or not the specified capability is supported by the current <see cref="IrcClient" />.
    /// </summary>
    /// <param name="capName"></param>
    /// <returns></returns>
    public bool SupportsCapability(string capName)
    {
        return _enabledCapabilities.Contains(capName, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc cref="IrcConnection.Start" />
    public Task<bool> Start() => _connection.Start();

    /// <inheritdoc cref="IrcConnection.Stop" />
    public async Task<bool> Stop(string? reason = null)
    {
        reason = reason == null ? string.Empty : string.Concat(" :", reason);
        await SendAsync($"QUIT {reason}");
        
        return await _connection.Stop();
    }

    /// <inheritdoc cref="IrcConnection.Send" />
    public bool Send(string format, params object[] args)
    {
        _logger?.LogDebug($"-> {string.Format(format, args)}");
        return _connection.Send(format, args);
    }

    /// <inheritdoc cref="IrcConnection.SendAsync" />
    public Task<bool> SendAsync(string format, params object[] args)
    {
        _logger?.LogDebug($"-> {string.Format(format, args)}");
        return _connection.SendAsync(format, args);
    }

    #endregion

    #region Handlers and Callbacks

    protected virtual void OnConnect() 
    {
        if (!string.IsNullOrEmpty(_config.Password))
        {
            Send($"PASS {_config.Password}");
        }
        
        Send("CAP LS 302");
        
        Send($"USER {_config.Ident} 0 * :{_config.RealName}");
        Send($"NICK {_config.Nick}");
        
        _registrationLock.Wait();
    }
    
    /// <summary>
    /// Processes incoming data received from the socket.
    /// </summary>
    /// <param name="data">The raw data line received.</param>
    protected virtual void OnDataReceived(string data) 
    {
        if (data.StartsWith("PING", StringComparison.OrdinalIgnoreCase))
        {
            var response = data.Substring(data.IndexOf(':') + 1);
            Send("PONG {0}", response);
            
            // Early exit. We've already responded to PING
            // so we don't need to process anymore!
            return;
        }
        
        string? prefix = null;
        string? trailing = null;
        
        var prefixEnd = -1;
        var trailingStart = -1;
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (data.StartsWith('@'))
        {
            var nextToken = data.IndexOf(' ');
            var kvp = data.Substring(1, nextToken);
            var dict = kvp.Split(';').Select(item => item.Split('=', count: 2))
                          .ToDictionary(k => k[0], v => v.Length > 1 ? v[1] : string.Empty);
            
            foreach (var item in dict)
            {
                tags[item.Key] = item.Value;
            }
            
            // free the memory, theoretically
            dict.Clear();
            
            // Reset the incoming data so we don't have to modify the parsing code below.
            data = data.Substring(nextToken + 1);
        }
        
        if (data.StartsWith(':'))
        {
            prefixEnd = data.IndexOf(' ');
            prefix = data.Substring(1, prefixEnd - 1);
        }

        trailingStart = data.IndexOf(" :", StringComparison.Ordinal);
        if (trailingStart != -1)
        {
            trailing = data.Substring(trailingStart + 2);
        }

        var commandSeq = data.Substring(prefixEnd + 1, (trailingStart == -1 ? data.Length : trailingStart) - (prefixEnd + 1));
        var parts = commandSeq.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
        {
            _logger?.LogDebug($"*** Invalid data received! {data}");
            return;
        }
        
        var command = parts[0];
        var commandParams = parts.Skip(1).ToArray();

        if (int.TryParse(command, out var numeric))
        {
            OnIrcNumeric(numeric, prefix!, trailing!, commandParams, tags);
        }
        else if (command.Equals("CAP", StringComparison.OrdinalIgnoreCase) && _registrationLock.CurrentCount == 0)
        {
            var subCommand = commandParams[1];
            if (subCommand.Equals("LS", StringComparison.OrdinalIgnoreCase))
            {
                // We're waiting for registration to complete, so this is a priority response.
                if (RequestedCapabilities.Count == 0) return;
                
                var availableCaps = trailing!.Split(' ').ToArray();
                // First get a list of capabilities that we can request and are available
                var req = RequestedCapabilities.Intersect(availableCaps).ToArray();
                    
                Send($"CAP REQ :{string.Join(' ', req)}");
            }
            else if (subCommand.Equals("ACK", StringComparison.OrdinalIgnoreCase))
            {
                var confirmedCaps = trailing!.Split(' ');
                _enabledCapabilities.AddRange(confirmedCaps);
                CapAckReceivedEvent?.Invoke(this, new CapAckReceivedEventArgs(_enabledCapabilities.ToArray()));
                Send("CAP END");
                
                // Check if we're waiting, and let it finish.
                var waiting = _registrationLock.Wait(0);
                if (waiting)
                {
                    _registrationLock.Release();
                }
            }
            else if (subCommand.Equals("NAK", StringComparison.OrdinalIgnoreCase))
            {
                // We shouldn't have to worry about NAK's if the above code works fine, but I guess better safe than sorry.
                // 
                // I guess, if we can't use one or more capabilities, we're done here and can send CAP END.
                // 
                // The reason we ignore NAK and mark the connection as not supporting ANY capabilities
                // is because the specification isn't required to enumerate the incorrect ones that
                // we requested.
                //
                // The specification really should force servers to enumerate these values to let us know which
                // ones are incorrect and which ones are incorrect.

                Send("CAP END");
                _registrationLock.Release();

                OnError($"Unable to request the following capabilities: {trailing}");
            }
        }
        else if (command.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            OnError(trailing!);
        }
        else
        {
            OnCommand(command, prefix!, trailing!, commandParams, tags);
        }
    }
    
    /// <summary>
    /// Processes an IRC command received from the IRC server.
    /// </summary>
    /// <param name="command">The base command received, i.e., PRIVMSG, NOTICE, etc.</param>
    /// <param name="prefix">The source of the command.</param>
    /// <param name="trailing">The value after the command's parameters, marked by a colon.</param>
    /// <param name="commandParams">An array of parameters between the command and its trailing data.</param>
    /// <param name="tags">Any message tags received with the command.</param>
    protected virtual void OnCommand(string command, string prefix, string trailing, string[] commandParams, IDictionary<string, string> tags) 
    {
        if (command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase)
            && trailing.StartsWith('\x01') && trailing.EndsWith('\x01'))
        {
            OnCtcpReceived(prefix, trailing.Trim('\x01').ToLower());
        }
        else if (command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase))
        {
            var target = commandParams[0];
            if (IsChannelName(target))
            {
                OnChannelMessageReceived(prefix, target, trailing, false, tags);
            }
            else
            {
                OnPrivateMessageReceived(prefix, trailing, false, tags);
            }
        }
        else if (command.Equals("NOTICE", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: Handle CTCP replies eventually.
            // Ignore CTCP replies FOR NOW.
            if (trailing.StartsWith('\x01') && trailing.EndsWith('\x01')) return;
            if (!prefix.Contains('!'))
            {
                OnServerNoticeReceived(prefix, trailing);
                return;
            }

            var target = commandParams[0];
            if (IsChannelName(target))
            {
                OnChannelMessageReceived(prefix, target, trailing, notice: true, tags);
            }
            else
            {
                OnPrivateMessageReceived(prefix, trailing, notice: true, tags);
            }
        }
        else if (command.Equals("JOIN", StringComparison.OrdinalIgnoreCase))
        {
            OnJoin(trailing, prefix);
        }
        else if (command.Equals("PART", StringComparison.OrdinalIgnoreCase))
        {
            OnPart(trailing, prefix);
        }
        else if (command.Equals("MODE", StringComparison.OrdinalIgnoreCase))
        {
            var target = commandParams[0];
            if (!IsChannelName(target))
            {
                // TODO: Self modes. Ignored for now.
                return;
            }

            var modes = commandParams.Length > 1 ? commandParams[1] : trailing;
            var otherParams = commandParams.Skip(2).Concat(trailing.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
            
            OnChannelMode(prefix, target, modes, otherParams);
        }
        else if (command.Equals("KICK", StringComparison.OrdinalIgnoreCase))
        {
            var channel = commandParams[0];
            var target = commandParams[1];
            var comment = trailing;
            OnKick(prefix, channel, target, comment);
        }
        else if (command.Equals("TOPIC", StringComparison.OrdinalIgnoreCase))
        {
            var channel = commandParams[0];
            var topic = trailing;
            OnTopicChanged(channel, topic);
        }
        else
        {
            _logger?.LogDebug($"<- ({prefix}) {command} [{string.Join(", ", commandParams)}] ({trailing})");
        }
    }
    
    /// <summary>
    /// Handles the welcome packet (numeric 001) received from the IRC server.
    /// </summary>
    protected virtual void OnConnectionEstablished()
    {
        ConnectionEstablishedEvent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles a message event received from the IRC server targeting a channel.
    /// </summary>
    /// <param name="prefix">The source of the message.</param>
    /// <param name="channel">The target channel of the message.</param>
    /// <param name="message">The message itself.</param>
    /// <param name="notice">Whether or not the message was a NOTICE or PRIVMSG.</param>
    /// <param name="tags">Any tags that the message contained.</param>
    protected virtual void OnChannelMessageReceived(string prefix, string channel, string message, bool notice = false, IDictionary<string, string>? tags = null)
    {
        ChannelMessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, prefix, channel, notice, tags));
    }

    /// <summary>
    /// Processes modes applied to a channel from a user.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="channelName"></param>
    /// <param name="modeString"></param>
    /// <param name="parameters"></param>
    protected virtual void OnChannelMode(string prefix, string channelName, string modeString, string[] parameters)
    {
        if (!TryGetChannel(channelName, out var channel)) return;
        
        foreach (var item in ParseChannelModes(modeString, parameters))
        {
            if (item.Type == ModeType.Access)
            {
                // Bail (continue) if we can't find the user.
                if (!channel.TryGetUserModes(item.Parameter, out var channelUser)) continue;
                
                var prefixIdx = _prefixModes.IndexOf(item.Mode);
                var prefixSymbol = _prefixSymbols[prefixIdx];
                
                var modes = channelUser.Modes;
                if (string.IsNullOrEmpty(modes))
                {
                    modes = string.Empty;
                }

                modes = item.IsSet ? modes += prefixSymbol : modes.Replace(prefixSymbol.ToString(), string.Empty);
                
                // Reorder the modes according to RPL_ISUPPORT's order.
                // 
                // By doing so, the user can simply taking modes[0] and be assured
                // that they have they highest access for the user.
                modes = new string(modes.OrderBy(ch => _prefixSymbols.IndexOf(ch)).Distinct().ToArray());
                
                channel.AddOrUpdateUser(channelUser.User, modes);
            }
            else
            {
                AddOrUpdateModeOnChannel(channelName, new ChannelMode(item.Mode, item.Type, item.Parameter), remove: !item.IsSet);
            }
        }
    }
    
    /// <summary>
    /// Processes simple client-to-client protocol (CTCP) messages excluding DCC events, primarily for IRC security scans that look for valid version replies. 
    /// </summary>
    /// <param name="prefix">The source of the CTCP event.</param>
    /// <param name="ctcpEvent">The CTCP event name.</param>
    protected virtual void OnCtcpReceived(string prefix, string ctcpEvent)
    {
        // Filter out DCC requests.
        if (ctcpEvent.StartsWith("DCC", StringComparison.OrdinalIgnoreCase)) return;

        var source = IrcSource.FromPrefix(prefix);
        var ctcpParams = string.Empty; 
        
        if (ctcpEvent.Contains(' '))
        {
            ctcpParams = ctcpEvent.Substring(ctcpEvent.IndexOf(' ') + 1);
            ctcpEvent = ctcpEvent.Substring(0, ctcpEvent.IndexOf(' '));
        }

        if (!Enum.TryParse(ctcpEvent, true, out CtcpEvent ctcp))
        {
            _logger?.LogWarning($"Unrecognized CTCP event? {ctcpEvent} (from {prefix})");
            return;
        }
        
        var args = new CtcpReceivedEventArgs(prefix, ctcp);
        CtcpReceivedEvent?.Invoke(this, args);
        
        string response;
        if (args.Cancel && !string.IsNullOrEmpty(args.Message))
        {
            response = args.Message;
        }
        else if (args.Cancel) return;
        else
        {
            switch (ctcp)
            {
                case CtcpEvent.Finger:
                    response = "Buy me dinner first...";
                    break;
                case CtcpEvent.Ping:
                    response = ctcpParams;
                    break;
                case CtcpEvent.Time:
                    response = DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy");
                    break;
                case CtcpEvent.Version:
                default:
                    response = Version;
                    break;
            }
        }

        Send($"NOTICE {source} :\x01{ctcp.ToString().ToUpper()} {response}\x01");
    }
    
    /// <summary>
    /// Handles errors detected in the IRC stream, usually via an ERROR command but some internal errors are filtered through this.
    /// </summary>
    /// <param name="message">The error message received.</param>
    protected virtual void OnError(string message)
    {
        ErrorReceivedEvent?.Invoke(this, new IrcErrorEventArgs(message));
    }
    
    /// <summary>
    /// Processes IRC numerics received from the IRC server.
    /// </summary>
    /// <param name="numeric">The numeric identifier.</param>
    /// <param name="prefix">The source of the numeric, likely a server.</param>
    /// <param name="trailing">The trailing data after the numeric's parameters, marked by a colon.</param>
    /// <param name="parameters">The parameters between the numeric and its trailing data.</param>
    /// <param name="tags">Any message tags associated with the message.</param>
    protected virtual void OnIrcNumeric(int numeric, string prefix, string trailing, string[] parameters, IDictionary<string, string> tags)
    {        
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (numeric == 1) 
        {
            OnConnectionEstablished();
        }
        else if (numeric == 324)
        {
            var channel = parameters[1];
            string modeString;
            string[] modeParams;
            
            if (parameters.Length == 2)
            {
                modeString = trailing;
                modeParams = [];
            }
            else if (parameters.Length == 3)
            {
                modeString = parameters[2];
                modeParams = trailing.Split(' ');
            }
            else
            {
                throw new InvalidOperationException($"Unknown case of numeric 324: {string.Join(',', parameters)} -> {trailing}");
            }
            
            foreach(var item in ParseChannelModes(modeString, modeParams)) 
            {
                if (item.Type != ModeType.Access && item.Type != ModeType.User)
                {
                    AddOrUpdateModeOnChannel(channel, new ChannelMode(item.Mode, item.Type, item.Parameter));
                }
            }
        }
        else if (numeric == 331 || numeric == 332)
        {
            var channel = parameters[1];
            var topic = trailing;
            OnTopicChanged(channel, topic);
        }
        else if (numeric == 353)
        {
            var channel = parameters[2];
            OnNamesReplyReceived(channel, trailing);
        }
        else if (numeric == 372) // MOTD
        {
            _motd.AppendLine(trailing);
        }
        else if (numeric == 376) // End of MOTD
        {
            OnMotdReceived(_motd.ToString());
            _motd.Clear();
        }
        else if (numeric == 005)
        {
            //   Skip the client name, then split the parameters into key value pairs,
            // setting singular values as keys with empty values.
            var serverSettings = parameters.Skip(1)
                                           .Select(item => item.Split('=', count: 2))
                                           .ToDictionary(item => item[0],
                                               item => item.Length > 1 ? item[1] : string.Empty);
                
            // Reassign and merge the server features dictionary with the updated list we just received. 
            _serverFeatureSupport = _serverFeatureSupport.Concat(serverSettings)
                                                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        else if (_lastNumeric == 005 && !_serverFeatureEventFired)
        {
            // We're starting to receive new lines, so fire off ISUPPORT.
            HandleReplyISupportReceived();
        }
        else
        {
            var source = IrcSource.FromPrefix(prefix);
            var paramList = string.Join(", ", parameters);
            _logger?.LogDebug($"<- {numeric:000} ({source}) ({paramList}): {trailing}");
        }
            
        _lastNumeric = numeric;
    }
    
    /// <summary>
    /// Handles kick events received from the IRC server.
    /// </summary>
    /// <param name="userPrefix">The source of the kick event.</param>
    /// <param name="channelName">The channel where the kick event originated.</param>
    /// <param name="target">The client who was kicked.</param>
    /// <param name="reason">The reason, if available, that the target was kicked.</param>
    protected virtual void OnKick(string userPrefix, string channelName, string target, string reason)
    {
        KickEvent?.Invoke(this, new KickEventArgs(channelName, userPrefix, target, reason));
        
        // Similar to PART, if the target of this event is the client, just unregister the channel.
        var isSelf = target.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (isSelf)
        {
            RemoveChannel(channelName);
            return;
        }
        
        if (_channels.TryGetValue(channelName, out var channel))
        {
            channel.RemoveUser(userPrefix);
        }
    }
    
    /// <summary>
    /// Fires off the event for when the server sends the end numeric for message of the day.
    /// </summary>
    /// <param name="motd">The message of the day buffer.</param>
    protected virtual void OnMotdReceived(string motd) 
    {
        MotdReceivedEvent?.Invoke(this, new MotdEventArgs(motd));
    }

    /// <summary>
    /// Processes a list of names and prefixes for a specified channel.
    /// </summary>
    /// <param name="channelName">The channel in which this nick list.</param>
    /// <param name="nickList">The list of nicks and prefixes.</param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void OnNamesReplyReceived(string channelName, string nickList)
    {
        if (!TryGetChannel(channelName, out var channel)) return;
        if (SupportsCapability(IrcV3Capabilities.MultiPrefix) && _multiPrefixNames != null)
        {
            var matches = _multiPrefixNames.Matches(nickList);
            foreach (Match m in matches) 
            {
                // little magic strings - not pretty but works.
                channel.AddOrUpdateUser(m.Groups["prefix"].ToString(), m.Groups["access"].ToString());
            }
        }
        else if (_multiPrefixNames == null)
        {
            throw new InvalidOperationException("IRCv3 multi-prefix capability enabled, but the regex is unset.");
        }
        else
        {
            var users = nickList.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var user in users)
            {
                var prefix = user[0];
                string username;

                if (_prefixSymbols.Contains(prefix))
                {
                    username = user.Substring(1);
                }
                else
                {
                    username = user;
                    prefix = '\0';
                }

                var accessModes = prefix == '\0' ? string.Empty : prefix.ToString();
                channel.AddOrUpdateUser(username, accessModes);
            }
        }
    }
    
    /// <summary>
    /// Represents a core event handler that processes a user joining a channel the <see cref="IrcClient" /> monitors.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="userPrefix"></param>
    protected virtual void OnJoin(string channelName, string userPrefix)
    {
        // Because we create the channel, this will always return true if the 
        if (!TryGetChannel(channelName, out var channel, createNew: true)) return;

        var source = IrcSource.FromPrefix(userPrefix);
        var nick = userPrefix;
        if (!SupportsCapability(IrcV3Capabilities.UserHostInNames))
        {
            nick = source.Nick;
        }
        
        if (!channel.TryGetUserModes(nick, out _))
        {
            channel.AddOrUpdateUser(nick);
        }
        
        JoinEvent?.Invoke(this, new JoinPartEventArgs(channelName, userPrefix));

        var isSelf = source.Nick.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (isSelf)
        {
            Send($"MODE {channelName}");
        }
    }
    
    /// <summary>
    /// Represents a core event handler that processes a user leaving a channel the <see cref="IrcClient" /> monitors.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="userPrefix"></param>
    protected virtual void OnPart(string channelName, string userPrefix) 
    {
        // Fire the event event regardless if it's us.
        PartEvent?.Invoke(this, new JoinPartEventArgs(channelName, userPrefix));
        var source = IrcSource.FromPrefix(userPrefix);
        
        var isSelf = source.Nick.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (isSelf)
        {
            // If this is us leaving a channel, just remove it.
            RemoveChannel(channelName);
            return;
        }

        if (!TryGetChannel(channelName, out var channel)) return;
        if (channel.TryGetUserModes(userPrefix, out _))
        {
            channel.RemoveUser(userPrefix);
        }
    }
    
    protected virtual void OnPrivateMessageReceived(string prefix, string message, bool notice = false, IDictionary<string, string>? tags = null) 
    {
        PrivateMessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, prefix, notice, tags));
    }

    protected virtual void HandleReplyISupportReceived()
    {
        // Fire the event
        ServerFeaturesReceivedEvent?.Invoke(this, new ServerFeaturesReceivedEventArgs(_serverFeatureSupport));
        _serverFeatureEventFired = true;

        // TODO: Actually process what we need out of this here before deleting it.
        // Process RPL_ISUPPORT for our needs here
        if (_serverFeatureSupport.TryGetValue("CHANTYPES", out var channelTypes))
        {
            _channelTypes = channelTypes;
        }
        
        if (_serverFeatureSupport.TryGetValue("PREFIX", out var prefix))
        {
            var endModesToken = prefix.IndexOf(')');
            _prefixModes = prefix.Substring(1, endModesToken - 1);
            _prefixSymbols = prefix.Substring(endModesToken + 1);
            
            if (SupportsCapability(IrcV3Capabilities.MultiPrefix) && _multiPrefixNames == null)
            {
                _multiPrefixNames = new Regex(@$"(?<access>[{_prefixSymbols}]*)(?<prefix>\S+)", RegexOptions.Compiled);
            }
        }
        
        if (_serverFeatureSupport.TryGetValue("CHANMODES", out var cModes))
        {
            var chanModes = cModes.Split(',');
            
            // Invalid sequence
            if (chanModes.Length != 4) return;

            var listModes = chanModes[0];
            var modesWithParam = chanModes[1];
            var modesWithParamsWhenSet = chanModes[2];
            var modesWithNoParam = chanModes[3];

            _chanModes = new ChannelModes(listModes, modesWithParam, modesWithParamsWhenSet, modesWithNoParam);
        }
    }
    
    protected virtual void OnServerNoticeReceived(string source, string message)
    {
        ServerNoticeReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, source, notice: true));
    }
    
    protected virtual void OnTopicChanged(string channelName, string topic)
    {
        if (!_channels.TryGetValue(channelName, out var channel))
        {
            channel = AddChannel(channelName);
        }
        
        var oldTopic = channel.Topic;
        channel.Topic = topic;
        TopicChangedEvent?.Invoke(this, new TopicChangedEventArgs(channelName, topic, oldTopic));
    }

    #endregion
}
