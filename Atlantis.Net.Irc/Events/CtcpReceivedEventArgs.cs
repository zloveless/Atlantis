using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

[PublicAPI]
public class CtcpReceivedEventArgs : EventArgs 
{
    public CtcpReceivedEventArgs(string prefix, CtcpEvent eventType)
    {
        Source = prefix;
        Event = eventType;
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the default response should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
    
    /// <summary>
    /// Gets a value indicating the origin of the CTCP request.
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// Gets a value representing the type of CTCP request.
    /// </summary>
    public CtcpEvent Event { get; }
    
    /// <summary>
    /// Gets or sets a value representing the overridden response.
    /// </summary>
    public string Message { get; set; }
}
