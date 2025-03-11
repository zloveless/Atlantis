using Atlantis.Net.Irc.Events;
using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

// TODO: Move all the events to a related interface somehow.

[PublicAPI]
public interface IIrcProtocolHandler
{
    /// <summary>
    /// Event fired when an IRC client connection receives numeric RPL_WELCOME (001).
    /// </summary>
    public event EventHandler ConnectionEstablishedEvent;

    /// <summary>
    /// Event fired when the IRC connection receives an ERROR command.
    /// </summary>
    public event EventHandler<IrcErrorEventArgs> ErrorReceivedEvent; 

    /// <summary>
    /// Event fired at the end of the MOTD transmission.
    /// </summary>
    public event EventHandler<MotdEventArgs> MotdReceivedEvent;

    /// <summary>
    /// Event fired after the last RPL_ISUPPORT (005) line received. Multiple lines buffered into a single event fire.
    /// </summary>
    public event EventHandler<ServerFeaturesReceivedEventArgs> ReplyISupportReceivedEvent;
    
    /// <summary>
    /// Processes a line received from the <see cref="IrcConnection" />.
    /// </summary>
    /// <param name="connection">The <see cref="IrcConnection"/> where data was received.</param>
    /// <param name="data">The line received from the connection</param>
    void Process(IrcConnection connection, string data);
}