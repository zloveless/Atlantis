using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public record ChannelUser(string User, string Modes);

/// <summary>
/// Represents a grouping of channel modes from RPL_ISUPPORT.
/// </summary>
/// <param name="ListModes">Modes that add or remove an address to or from a list. These modes MUST always have a parameter when sent from the server to a client. A client MAY issue the mode without an argument to obtain the current contents of the list.</param>
/// <param name="ModesWithParameter">Modes that change a setting on a channel. These modes MUST always have a parameter.</param>
/// <param name="ModesWithParametersWhenSet">Modes that change a setting on a channel. These modes MUST have a parameter when being set, and MUST NOT have a parameter when being unset.</param>
/// <param name="ModesWithNoParameter">Modes that change a setting on a channel. These modes MUST NOT have a parameter.</param>
[PublicAPI]
public record ChannelModes(string ListModes, string ModesWithParameter, string ModesWithParametersWhenSet, string ModesWithNoParameter);

/// <summary>
/// Represents a generic abstraction of a mode parsed from a MODE command.
/// </summary>
/// <param name="Mode"></param>
/// <param name="Parameter"></param>
/// <param name="IsSet"></param>
/// <param name="Type"></param>
[PublicAPI]
public record GenericMode(char Mode, string Parameter, bool IsSet, ModeType Type);
