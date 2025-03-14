using Atlantis.Net.Irc;

var config = IrcClientConfiguration.New("GTestClient");
var client = new IrcClient(config)
{
    HostName = "irc.cncirc.net",
    UseSsl = true,
    Port = 9999
};

client.EnableV3 = true;
client.StrictNames = true;

client.CapAckReceivedEvent += (sender, e) =>
{
    //Console.WriteLine($"*** Received CAP ACK with the following capabilities: {string.Join(", ", e.Capabilities)}");
};

client.ConnectionEstablishedEvent += (sender, e) => 
{
    Console.WriteLine("Connected to IRC!");
    client.Send("JOIN #genesis");
};

client.ChannelMessageReceivedEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        Console.WriteLine($"NOTICE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
    else
    {
        Console.WriteLine($"MESSAGE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
    
    if (!e.IsNotice && e.Message.StartsWith("!hello")) 
    {
        client.Send($"PRIVMSG {e.Target} :Hello world");
    }
    else if (!e.IsNotice && e.Message.StartsWith("!modes"))
    {
        var modes = client.GetChannelUserModes(e.Target, e.Source);
        client.Send($"PRIVMSG {e.Target} :Hello {source.Nick}, your mode(s) for {e.Target} are: {modes}");
    }
};

client.PrivateMessageReceivedEvent += (sender, e) =>
{
    /*var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        Console.WriteLine($"NOTICE({source}, {e.Tags?.Count ?? 0}): {e.Message}");
    }
    else
    {
        Console.WriteLine($"MESSAGE({source}, {e.Tags?.Count ?? 0}): {e.Message}");
    }*/
};

client.CtcpReceivedEvent += (sender, e) =>
{
    // Console.WriteLine($"Received CTCP ({e.Event}) from {e.Source}");
};

client.MotdReceivedEvent += (sender, e) =>
{
    // Console.WriteLine($"Received MOTD: Length = {e.Motd.Length}");
};

client.ServerNoticeReceivedEvent += (sender, e) =>
{
    // Console.WriteLine($"SNOTICE({e.Source}): {e.Message}");
};

client.ServerFeaturesReceivedEvent += (sender, e) =>
{
    // Console.WriteLine($"Received RPL_ISUPPORT: {JsonConvert.SerializeObject(e.ServerFeatures)}");
};

client.ErrorReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"ERROR: {e.Message}");
};

client.JoinEvent += (sender, e) =>
{
    Console.WriteLine($"JOIN({e.Channel}): {e.UserPrefix}");
};

client.PartEvent += (sender, e) =>
{
    Console.WriteLine($"PART({e.Channel}): {e.UserPrefix}");
};

await client.Start();

Console.WriteLine("Press <CTRL+C> to cancel...");
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Terminating...");
    e.Cancel = true;
    client.Stop("Exiting...").Wait();
};
