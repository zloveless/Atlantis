using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

/// <summary>
/// Represents a message received.
/// </summary>
[PublicAPI]
public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(string message, string prefix, string target, bool notice = false) : this(message, prefix, notice)
    {
        Target = target;
    }

    public MessageReceivedEventArgs(string message, string prefix, bool notice = false)
    {
        Message = message;
        Source = prefix;
        IsNotice = notice;
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
}
