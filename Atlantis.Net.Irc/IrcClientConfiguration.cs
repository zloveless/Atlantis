using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public class IrcClientConfiguration
{
    private IrcClientConfiguration() { }
    
    /// <summary>
    /// Gets the nickname of the client.
    /// </summary>
    public string Nick { get; private set; }
    
    /// <summary>
    /// Gets the ident info of the client.
    /// </summary>
    public string Ident { get; private set; }
    
    /// <summary>
    /// Gets the real name of the client.
    /// </summary>
    public string RealName { get; private set; }
    
    /// <summary>
    /// Gets the password for the connection to send during registration.
    /// </summary>
    public string Password { get; private set; }

    /// <summary>
    /// Creates a new <see cref="IrcClientConfiguration" /> using the specified parameters.
    /// </summary>
    /// <param name="nick"></param>
    /// <param name="password"></param>
    /// <param name="ident"></param>
    /// <param name="realName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IrcClientConfiguration New(string nick, string password = "", string? ident = null, string? realName = null)
    {        
        if (string.IsNullOrEmpty(nick))
        {
            throw new ArgumentNullException(nameof(nick));
        }

        return new IrcClientConfiguration
        {
            Nick = nick,
            Ident = ident ?? nick.ToLower(),
            RealName = realName ?? nick.ToLower(),
            Password = password
        };
    }
}
