using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

[PublicAPI]
public class CtcpReceivedEventArgs : EventArgs 
{
    public CtcpReceivedEventArgs(string prefix, CtcpEvent eventType, bool reply = false, IDictionary<string, string>? tags = null, string? parameters = null)
    {
        Event = eventType;
        IsReply = reply;
        Parameters = parameters;
        Source = prefix;
        Tags = tags?.AsReadOnly();
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the default response should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
    
    /// <summary>
    /// Gets a value representing the type of CTCP request.
    /// </summary>
    public CtcpEvent Event { get; }
    
    /// <summary>
    /// Gets a value indicating whether the CTCP event was a reply.
    /// </summary>
    public bool IsReply { get; }

    /// <summary>
    /// Gets a value representing any parameters passed through from the reply. Note: this is only filled in for replies.
    /// </summary>
    public string? Parameters { get; }

    /// <summary>
    /// Gets or sets a value representing the overridden response.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets a value indicating the origin of the CTCP request.
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// Gets a value representing any message tags received in the CTCP event.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Tags { get; }
}
