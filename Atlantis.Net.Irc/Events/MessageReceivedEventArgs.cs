using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

/// <summary>
/// Represents a message received.
/// </summary>
[PublicAPI]
public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(string message, string prefix, string target, bool notice = false, IDictionary<string, string>? tags = null) : this(message, prefix, notice, tags)
    {
        Target = target;
    }

    public MessageReceivedEventArgs(string message, string prefix, bool notice = false, IDictionary<string, string>? tags = null)
    {
        Message = message;
        Source = prefix;
        IsNotice = notice;
        Tags = tags;
    }
    
    /// <summary>
    /// Gets the message itself.
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Gets the source of the message.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the target of the message.
    /// </summary>
    public string? Target { get; } = null;
    
    /// <summary>
    /// Gets a value representing whether the message is a notice or privmsg.
    /// </summary>
    public bool IsNotice { get; }

    /// <summary>
    /// Gets a value representing the message tags associated with the message event.
    /// </summary>
    public IDictionary<string, string>? Tags { get; }
}