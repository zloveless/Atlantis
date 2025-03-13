using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public record ChannelUser(string User, string Modes);
