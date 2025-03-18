using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

/// <summary>
/// Represents information about an IRC KICK event. 
/// </summary>
[PublicAPI]
public class KickEventArgs : JoinPartEventArgs
{
    public KickEventArgs(string channel, string userPrefix, string target, string reason) : base(channel, userPrefix)
    {
        Target = target;
        Reason = reason;
    }

    /// <summary>
    /// Gets the target of the kick event.
    /// </summary>
    public string Target { get; }
    
    /// <summary>
    /// Gets the reason the user kicked the target.
    /// </summary>
    public string Reason { get; }
}
