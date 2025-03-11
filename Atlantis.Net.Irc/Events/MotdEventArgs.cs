namespace Atlantis.Net.Irc.Events;

public class MotdEventArgs: EventArgs 
{
    public MotdEventArgs(string motd)
    {
        Motd = motd;
    }
 
    /// <summary>
    /// Returns the MOTD received from the IRC server.
    /// </summary>
    public string Motd { get; }
}