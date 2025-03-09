using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI]
public static class IrcV3Capabilities
{
    public const string AccountNotify = "account-notify";
    public const string AccountTag = "account-tag";
    public const string AwayNotify = "away-notify";
    public const string Batch = "batch";
    public const string CapNotify = "cap-notify";
    public const string ChgHost = "chghost";
    public const string EchoMessage = "echo-message";
    public const string ExtendedJoin = "extended-join";
    public const string ExtendedMonitor = "extended-monitor";
    public const string InviteNotify = "invite-notify";
    public const string LabeledResponse = "labeled-response";
    public const string MessageTags = "message-tags";
    public const string MultiPrefix = "multi-prefix";
    public const string SetName = "setname";
    public const string UserHostInNames = "userhost-in-names";
}
