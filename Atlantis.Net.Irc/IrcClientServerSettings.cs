using JetBrains.Annotations;

namespace Atlantis.Net.Irc;


/// <summary>
/// Represents a collection of settings for the <see cref="IrcClient" />. 
/// </summary>
/// <param name="AwayLength">The maximum length of an away message.</param>
/// <param name="NickLength">The maximum a nickname can be.</param>
/// <param name="Network">The network name.</param>
/// <param name="BotMode">The mode which specifies a client is a bot.</param>
[PublicAPI]
public record IrcClientSupportsSettings(
    short AwayLength = 0,
    short NickLength = 0,
    string Network = "",
    char BotMode = '\0'
);
