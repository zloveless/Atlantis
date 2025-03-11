namespace Atlantis.Net.Irc;

public class CapAckReceivedEventArgs : EventArgs 
{
    public CapAckReceivedEventArgs(string[] caps)
    {
        Capabilities = caps;
    }
    
    public string[] Capabilities { get; }
}
