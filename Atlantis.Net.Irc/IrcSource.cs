using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public class IrcSource
{
    public IrcSource(string server)
    {
        Nick = null;
        User = null;
        Host = server;
    }

    public IrcSource(string nick, string ident, string host)
    {
        Nick = nick;
        User = ident;
        Host = host;
    }
    
    /// <summary>
    /// Gets a value representing the source's nick. 
    /// </summary>
    public string Nick { get; }
    
    /// <summary>
    /// Gets a value representing the source's user or ident.
    /// </summary>
    public string User { get; }
    
    /// <summary>
    /// Gets a value representing the source's host mask.
    /// </summary>
    public string Host { get; }
    
    /// <summary>
    /// Gets a value indicating whether the source is a server or a user. 
    /// </summary>
    public bool IsServer => string.IsNullOrEmpty(Nick) && !string.IsNullOrEmpty(Host);
    
    public override string ToString()
    {
        return IsServer ? Host : Nick;
    }
    
    /// <summary>
    /// Converts a prefix into a source.
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    [PublicAPI]
    public static IrcSource FromPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentException("Invalid prefix", nameof(prefix));
        }
        
        var nickPrefix = prefix.IndexOf('!');
        if (nickPrefix == -1)
        {
            return new IrcSource(prefix);
        }

        var hostIndex = prefix.IndexOf('@');
        var nick = prefix.Substring(0, nickPrefix);
        var ident = prefix.Substring(nickPrefix + 1, hostIndex - 1);
        var hostMask = prefix.Substring(hostIndex + 1);

        return new IrcSource(nick, ident, hostMask);
    } 
}
