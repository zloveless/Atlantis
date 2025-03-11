using System.Text;
using Atlantis.Net.Irc.Events;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Atlantis.Net.Irc;

[PublicAPI]
public class IrcClient
{
    public const string Version = "Atlantis.Net.Irc/5.0.0 (.NET 9.0)";

    public static readonly string[] SupportedCapabilities =
    [
        IrcV3Capabilities.EchoMessage,
        IrcV3Capabilities.MessageTags
    ];

    private string _channelTypes;
    
    private readonly IrcClientConfiguration _config;
    private readonly ILogger? _logger;
    private IrcConnection _connection;
    private readonly List<string> _requestedCapabilities = [];
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
    /// Event fired after the last RPL_ISUPPORT (005) line received. Multiple lines buffered into a single event fire.
    /// </summary>
    public event EventHandler<ServerFeaturesReceivedEventArgs> ServerFeaturesReceivedEvent;

    #endregion

    #region Methods
    
    /// <summary>
    /// Appends the specified capability
    /// </summary>
    /// <param name="cap"></param>
    /// <exception cref="ArgumentException"></exception>
    public void RequestCapability(string cap) 
    {
        if (!SupportedCapabilities.Contains(cap, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The specified capability is not supported by the IRC client.", nameof(cap));
        }
        
        // Only add it once
        if (!_requestedCapabilities.Contains(cap, StringComparer.OrdinalIgnoreCase))
        {
            _requestedCapabilities.Add(cap);
        }
    }
    
    /// <summary>
    /// Returns a value whether or not the specified target is a channel name or not.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool IsChannelName(string target)
    {
        return _channelTypes.Any(target.StartsWith);
    }

    /// <inheritdoc cref="IrcConnection.Start" />
    public Task<bool> Start() => _connection.Start();

    /// <inheritdoc cref="IrcConnection.Stop" />
    public Task<bool> Stop(string? reason = null) => _connection.Stop(reason ?? "Exiting");

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
        
        // TODO: Support IRCv3 CAP negotiation for servers that require it for security.
        // i.e., irc.cncirc.net (shameless plug).

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
            
            tags = tags.Concat(dict).ToDictionary();
            
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

        var numeric = -1;
        if (int.TryParse(command, out numeric))
        {
            OnIrcNumeric(numeric, prefix, trailing, commandParams, tags);
        }
        else if (command.Equals("CAP", StringComparison.OrdinalIgnoreCase) && _registrationLock.CurrentCount == 0)
        {
            var subCommand = commandParams[1];
            if (subCommand.Equals("LS", StringComparison.OrdinalIgnoreCase)) 
            {
                // We're waiting for registration to complete, so this is a priority response.
                if (_requestedCapabilities.Count != 0 && SupportedCapabilities.Length != 0)
                {
                    var availableCaps = trailing!.Split(' ').ToArray();
                    // First get a list of capabilities that we can request and are available
                    var req = _requestedCapabilities.Intersect(availableCaps).ToArray();
                
                    // Then filter that list by a list of supported capabilities.
                    var supported = SupportedCapabilities.Intersect(req).ToArray();
                    _connection.Send($"CAP REQ :{string.Join(' ', supported)}");
                }
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
                // This is because the specification isn't required to enumerate the bad 

                _connection.Send("CAP END");
                _registrationLock.Release();

                OnError($"Unable to request the following capabilities: {trailing}");
            }
        }
        else if (command.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            OnError(trailing);
        }
        else
        {
            OnCommand(command, prefix, trailing, commandParams, tags);
        }
    }
    
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
        else
        {
            Console.WriteLine($"<- ({prefix}) {command} [{string.Join(", ", commandParams)}] ({trailing})");
        }
    }
    
    protected virtual void OnConnectionEstablished()
    {
        ConnectionEstablishedEvent?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnChannelMessageReceived(string prefix, string channel, string message, bool notice = false, IDictionary<string, string>? tags = null)
    {
        ChannelMessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs(message, prefix, channel, notice, tags));
    }
    
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
            _logger?.LogDebug($"Unrecognized CTCP event? {ctcpEvent} (from {prefix})");
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
    
    protected virtual void OnError(string message)
    {
        ErrorReceivedEvent?.Invoke(this, new IrcErrorEventArgs(message));
    }
    
    protected virtual void OnIrcNumeric(int numeric, string prefix, string trailing, string[] parameters, IDictionary<string, string> tags)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (numeric == 1) 
        {
            OnConnectionEstablished();
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
    
    protected virtual void OnMotdReceived(string motd) 
    {
        MotdReceivedEvent?.Invoke(this, new MotdEventArgs(motd));
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
