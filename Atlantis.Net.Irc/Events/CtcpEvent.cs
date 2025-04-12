using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

[PublicAPI]
public enum CtcpEvent 
{
    Action,
    Finger,
    Ping,
    Time,
    Version,
}
