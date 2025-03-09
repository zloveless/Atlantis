using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public enum CtcpEvent 
{
    Finger,
    Ping,
    Time,
    Version,
}
