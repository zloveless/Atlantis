using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public enum ModeType
{
    /// <summary>
    ///     Channel mode has many separate parameters which can be by requesting +[x] where [x] is the mode with no parameters.
    /// </summary>
    List,

    /// <summary>
    ///     Channel mode that always takes a parameter, regardless whether it's set or unset.
    /// </summary>
    SetUnset,

    /// <summary>
    ///     Channel mode that requires a parameter only when being set, otherwise has no parameter.
    /// </summary>
    Set,

    /// <summary>
    ///     Channel mode should never have a parameter associated with it.
    /// </summary>
    NoParam,

    /// <summary>
    ///     Mode that grants a user access on a channel. The associated prefix should be stored with the user, not the channel.
    /// </summary>
    Access,

    /// <summary>
    ///     Generic mode representing a user mode that should be stored for the IrcClient (ourselves). Occurs when target and
    ///     source are the same value.
    /// </summary>
    User
}
