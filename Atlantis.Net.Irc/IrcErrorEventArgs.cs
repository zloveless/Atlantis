namespace Atlantis.Net.Irc;

public class IrcErrorEventArgs : EventArgs
{
    public IrcErrorEventArgs(string message)
    {
        Message = message;
    }
    
    /// <summary>
    /// Gets a value representing the error message received from the IRC connection.
    /// </summary>
    public string Message { get; }
}