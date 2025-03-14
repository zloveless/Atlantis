namespace Atlantis.Net.Irc.Events;

public class JoinPartEventArgs : EventArgs
{
    public JoinPartEventArgs(string channel, string userPrefix)
    {
        Channel = channel;
        UserPrefix = userPrefix;
    }
    
    /// <summary>
    /// Gets the channel in which the user joined or left.
    /// </summary>
    public string Channel { get; }
    
    /// <summary>
    /// Gets the user who joined or left a channel.
    /// </summary>
    public string UserPrefix { get; }
}
