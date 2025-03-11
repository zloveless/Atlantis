using JetBrains.Annotations;

namespace Atlantis.Net.Irc.Events;

[PublicAPI]
public enum CtcpEvent 
{
    Finger,
    Ping,
    Time,
    Version,
}
