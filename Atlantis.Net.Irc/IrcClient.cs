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

    private readonly Dictionary<string, List<ChannelUser>> _channelUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<ChannelMode>> _channelModes = new(StringComparer.OrdinalIgnoreCase);
    
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
        _connection = new IrcConnection(OnConnect, OnStop, OnDataReceived, stream => stream.AuthenticateAsClient(HostName));
    }

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether to attempt registration with capabilities.
    /// </summary>
    public bool EnableV3 { get; set; }
    
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
    
    /// <summary>
    /// Gets or sets a value indicating whether to request NAMES {channel} whenever an action (i.e., join, part, mode, etc.) occurs to affect user access.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For what it's worth, this property is mainly for testing and debugging the client during development when I do not have all commands being processed.
    ///     </para>
    ///     <para>
    ///         This property may not survive in a full release of 5.0.0 of this library.
    ///     </para> 
    /// </remarks>
    public bool StrictNames { get; set; }

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

    #endregion

    #region Methods

    /// <summary>
    /// Adds a new or updates an existing channel mode to the internal registrar.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="channelMode"></param>
    /// <param name="remove">Whether or not to remove the channel mode.</param>
    private void AddOrUpdateModeOnChannel(string channel, ChannelMode channelMode, bool remove = false) 
    {
        if (_channelModes.TryGetValue(channel, out var channelModes))
        {
            // Checks if the parameter for this mode is required and whether it's set.
            bool IsModeParameterRequired(ChannelMode cm) => cm.Type == ModeType.NoParam && cm.Parameter == null;

            // Checks whether the parameter is NOT null and if it matches the provided parameter.
            bool DoesRequiredParameterMatchProvidedParam(ChannelMode cm) => cm.Parameter != null &&
                                                                            cm.Parameter.Equals(channelMode.Parameter,
                                                                                StringComparison.OrdinalIgnoreCase);
            
            // Checks if the parameter is required and whether it matches the provided channelMode 
            bool DoesParameterMatch(ChannelMode cm) =>
                IsModeParameterRequired(cm) || DoesRequiredParameterMatchProvidedParam(cm);

            var current = channelModes.FirstOrDefault(cm => cm.Mode.Equals(channelMode.Mode) && DoesParameterMatch(cm));
            
            if (current != null)
            {
                channelModes.Remove(current);
            }
            
            // If remove was not requested, (re-) add the mode back to the channel modes list.
            if (!remove)
            {
                channelModes.Add(channelMode);
            }
        }
        else
        {
            _channelModes[channel] =
            [
                channelMode
            ];
        }
    }
    
    /// <summary>
    /// Adds the user's modes to the specified channel.
    /// </summary>
    /// <param name="channel">The channel to update.</param>
    /// <param name="user">The user's modes to update on the specified channel.</param>
    private void AddUserToChannel(string channel, ChannelUser user)
    {
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            var current =
                channelUsers.FirstOrDefault(u => u.User.Equals(user.User, StringComparison.OrdinalIgnoreCase));
            
            // Remove and override since it's a C# 'record' type.
            if (current != null)
            {
                channelUsers.Remove(current);
            }
            
            channelUsers.Add(user);
        }
        else
        {
            _channelUsers[channel] =
            [
                user
            ];
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
    /// <param name="channel">The channel to look up a user's access level.</param>
    /// <param name="userPrefix">The requested user's full prefix when looking up their access.</param>
    /// <exception cref="ArgumentNullException">Thrown when either of the two arguments are invalid values such as null or empty strings.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified channel does not exist in the <see cref="IrcClient" />'s internal channel registrar.</exception>
    /// <returns>The user's mode prefixes</returns>
    public string GetChannelUserModes(string channel, string userPrefix) 
    {
        if (string.IsNullOrEmpty(channel))
        {
            throw new ArgumentNullException(nameof(channel));
        }
        
        if (string.IsNullOrEmpty(userPrefix))
        {
            throw new ArgumentNullException(nameof(userPrefix));
        }
        
        if (!_channelUsers.ContainsKey(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }
        
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            return channelUsers
                   .FirstOrDefault(cu => cu.User.Equals(userPrefix, StringComparison.OrdinalIgnoreCase))?.Modes ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns a <see cref="ChannelUser" /> if they exist on the channel.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private ChannelUser? GetUserInChannelFromUserName(string channel, string userName) 
    {
        if (string.IsNullOrEmpty(channel))
        {
            throw new ArgumentNullException(nameof(channel));
        }
        
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }
        
        if (!_channelUsers.ContainsKey(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }
        
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            return channelUsers.FirstOrDefault(cu => cu.User.StartsWith(userName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
    
    /// <summary>
    /// Updates the specified user's modes on the specified channel.
    /// </summary>
    /// <param name="channel">The channel in which to update.</param>
    /// <param name="channelUser">The user's modes for the specified channel.</param>
    /// <exception cref="ArgumentNullException">Throw when channel is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified channel does not exist in the internal mapping.</exception>
    private void UpdateUserOnChannel(string channel, ChannelUser channelUser) 
    {
        if (string.IsNullOrEmpty(channel) || !IsChannelName(channel))
        {
            throw new ArgumentNullException(nameof(channel));
        }
        
        if (!_channelUsers.ContainsKey(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        if (!_channelUsers.TryGetValue(channel, out var channelUsers)) return;

        var search =
            channelUsers.FirstOrDefault(cu => cu.User.Equals(channelUser.User, StringComparison.OrdinalIgnoreCase));
        if (search != null)
        {
            channelUsers.Remove(search);
        }

        channelUsers.Add(channelUser);
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
    /// Returns whether or not the specified capability is supported by the current <see cref="IrcClient" />.
    /// </summary>
    /// <param name="capName"></param>
    /// <returns></returns>
    public bool SupportsCapability(string capName)
    {
        return EnableV3 && _enabledCapabilities.Contains(capName, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc cref="IrcConnection.Start" />
    public Task<bool> Start() => _connection.Start();

    /// <inheritdoc cref="IrcConnection.Stop" />
    public Task<bool> Stop(string? reason = null)
    {
        return _connection.Stop(reason ?? "Exiting");
    }

    /// <inheritdoc cref="IrcConnection.Send" />
    public bool Send(string format, params object[] args) => _connection.Send(format, args);
    
    /// <inheritdoc cref="IrcConnection.SendAsync" />
    public Task<bool> SendAsync(string format, params object[] args) => _connection.SendAsync(format, args);

    #endregion

    #region Handlers and Callbacks

    protected virtual void OnConnect() 
    {
        if (!string.IsNullOrEmpty(_config.Password))
        {
            _connection.Send($"PASS {_config.Password}");
        }

        _connection.Send($"USER {_config.Ident} 0 * :{_config.RealName}");
        _connection.Send($"NICK {_config.Nick}");

        if (!EnableV3)
        {
            _registrationLock.Release();
            return;
        }
        
        _connection.Send("CAP LS 302");
        _registrationLock.Wait();
    }
    
    protected virtual async Task<bool> OnStop(string? reason = null)
    {
        reason = reason == null ? string.Empty : string.Concat(" :", reason);
        await _connection.SendAsync($"QUIT {reason}");
        return true;
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
            _connection.Send("PONG {0}", response);
            
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
                    
                _connection.Send($"CAP REQ :{string.Join(' ', req)}");
            }
            else if (subCommand.Equals("ACK", StringComparison.OrdinalIgnoreCase))
            {
                var confirmedCaps = trailing!.Split(' ');
                _enabledCapabilities.AddRange(confirmedCaps);
                CapAckReceivedEvent?.Invoke(this, new CapAckReceivedEventArgs(_enabledCapabilities.ToArray()));
                _connection.Send("CAP END");
                _registrationLock.Release();
            }
            else if (subCommand.Equals("NAK", StringComparison.OrdinalIgnoreCase))
            {
                // We shouldn't have to worry about NAK's if the above code works fine, but I guess better safe than sorry.
                // I guess, if we can't use one or more capabilities, we're done here and can send CAP END.
                // 
                // This is because the specification isn't required to enumerate the bad.

                _connection.Send("CAP END");
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
            
            var modes = commandParams[1];
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
    /// <param name="channel"></param>
    /// <param name="modeString"></param>
    /// <param name="parameters"></param>
    protected virtual void OnChannelMode(string prefix, string channel, string modeString, string[] parameters)
    {
        if (StrictNames)
        {
            Send($"NAMES {channel}");
        }
        
        foreach (var item in ParseChannelModes(modeString, parameters))
        {
            if (item.Type == ModeType.Access)
            {
                var prefixIdx = _prefixModes.IndexOf(item.Mode);
                var prefixSymbol = _prefixSymbols[prefixIdx];
                var channelUser = GetUserInChannelFromUserName(channel, item.Parameter);
                
                if (channelUser == null)
                {
                    // We somehow don't have a record of them... BUG!
                    continue;
                }
                
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
                modes = new string(modes.OrderBy(ch => _prefixSymbols.IndexOf(ch)).ToArray());
                
                UpdateUserOnChannel(channel, channelUser with { Modes = modes });
            }
            else
            {
                AddOrUpdateModeOnChannel(channel, new ChannelMode(item.Mode, item.Type, item.Parameter), remove: !item.IsSet);
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
        if (args.Cancel) return;
        
        var response = string.Empty;
        switch(ctcp) 
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

        _connection.Send($"NOTICE {source} :\x01{ctcp.ToString().ToUpper()} {response}\x01");
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
    
    protected virtual void OnKick(string userPrefix, string channel, string target, string reason)
    {
        KickEvent?.Invoke(this, new KickEventArgs(channel, userPrefix, target, reason));
        
        var isSelf = target.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (isSelf)
        {
            _channelUsers.Remove(channel);
            _channelModes.Remove(channel);
            return;
        }
        
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            var nick = userPrefix;
            if (!SupportsCapability(IrcV3Capabilities.UserHostInNames))
            {
                nick = IrcSource.FromPrefix(userPrefix).Nick;
            }

            var search = channelUsers.FirstOrDefault(u => u.User.Equals(nick, StringComparison.OrdinalIgnoreCase));
            if (search != null)
            {
                channelUsers.Remove(search);
            }
        }
        
        if (StrictNames)
        {
            Send($"NAMES {channel}");
        }
    }
    
    protected virtual void OnMotdReceived(string motd) 
    {
        MotdReceivedEvent?.Invoke(this, new MotdEventArgs(motd));
    }
    
    protected virtual void OnNamesReplyReceived(string channel, string nickList)
    {
        if (SupportsCapability(IrcV3Capabilities.MultiPrefix) && _multiPrefixNames != null)
        {
            var matches = _multiPrefixNames.Matches(nickList);
            foreach (Match m in matches) 
            {
                // little magic strings - not pretty but works.
                AddUserToChannel(channel, new ChannelUser(m.Groups["prefix"].ToString(), m.Groups["access"].ToString()));
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

                AddUserToChannel(channel, new ChannelUser(username, prefix == '\0' ? string.Empty : prefix.ToString()));
            }
        }
    }
    
    /// <summary>
    /// Represents a core event handler that processes a user joining a channel the <see cref="IrcClient" /> monitors.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="userPrefix"></param>
    protected virtual void OnJoin(string channel, string userPrefix) 
    {
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            var nick = userPrefix;
            if (!SupportsCapability(IrcV3Capabilities.UserHostInNames))
            {
                nick = IrcSource.FromPrefix(userPrefix).Nick;
            }

            var current = channelUsers.FirstOrDefault(u => u.User.Equals(nick, StringComparison.OrdinalIgnoreCase));
            if (current == null)
            {
                AddUserToChannel(channel, new ChannelUser(nick, string.Empty));
            }
        }
        
        JoinEvent?.Invoke(this, new JoinPartEventArgs(channel, userPrefix));

        var isSelf = IrcSource.FromPrefix(userPrefix).Nick.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (StrictNames || isSelf)
        {
            Send($"NAMES {channel}");
        }
        
        if (isSelf)
        {
            Send($"MODE {channel}");
        }
    }
    
    /// <summary>
    /// Represents a core event handler that processes a user leaving a channel the <see cref="IrcClient" /> monitors.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="userPrefix"></param>
    protected virtual void OnPart(string channel, string userPrefix) 
    {
        // Fire the event event regardless if it's us.
        PartEvent?.Invoke(this, new JoinPartEventArgs(channel, userPrefix));
        
        var isSelf = IrcSource.FromPrefix(userPrefix).Nick.Equals(_config.Nick, StringComparison.OrdinalIgnoreCase);
        if (isSelf)
        {
            // If this is us leaving a channel, just remove it.
            _channelUsers.Remove(channel);
            _channelModes.Remove(channel);
            return;
        }
        
        if (_channelUsers.TryGetValue(channel, out var channelUsers))
        {
            var nick = userPrefix;
            if (!SupportsCapability(IrcV3Capabilities.UserHostInNames))
            {
                nick = IrcSource.FromPrefix(userPrefix).Nick;
            }

            var search = channelUsers.FirstOrDefault(u => u.User.Equals(nick, StringComparison.OrdinalIgnoreCase));
            if (search != null)
            {
                channelUsers.Remove(search);
            }
        }
        
        if (StrictNames)
        {
            Send($"NAMES {channel}");
        }
    }
    
    protected virtual void OnPrivateMessageReceived(string prefix, string message, bool notice = false, IDictionary<string, string>? tags = null) 
    {
        PrivateMessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, prefix, notice, tags));
    }

    protected virtual void HandleReplyISupportReceived()
    {
        // Fire the event
        OnServerFeaturesReceived(_serverFeatureSupport);
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

    protected virtual void OnServerFeaturesReceived(IDictionary<string, string> serverFeatures)
    {
        ServerFeaturesReceivedEvent?.Invoke(this, new ServerFeaturesReceivedEventArgs(serverFeatures));
    }
    
    protected virtual void OnServerNoticeReceived(string source, string message)
    {
        ServerNoticeReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, source, notice: true));
    }

    #endregion
}
