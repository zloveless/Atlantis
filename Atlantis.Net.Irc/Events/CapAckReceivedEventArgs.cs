namespace Atlantis.Net.Irc.Events;

public class CapAckReceivedEventArgs : EventArgs 
{
    public CapAckReceivedEventArgs(string[] caps)
    {
        Capabilities = caps;
    }
    
    public string[] Capabilities { get; }
}
