using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

/// <summary>
/// Represents information about a channel's topic when it changes.
/// </summary>
[PublicAPI]
public class TopicChangedEventArgs : EventArgs 
{
    public TopicChangedEventArgs(string channel, string topic, string oldTopic)
    {
        Channel = channel;
        Topic = topic;
        OldTopic = oldTopic;
    }
    
    /// <summary>
    /// Represents the channel whose topic changed.
    /// </summary>
    public string Channel { get; }
    
    /// <summary>
    /// Represents the current (new) topic of the channel.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Represents the old topic the channel.
    /// </summary>
    public string OldTopic { get; }
}
