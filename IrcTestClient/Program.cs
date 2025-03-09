using Atlantis.Net.Irc;

var config = IrcClientConfiguration.New("GTestClient");
var client = new IrcClient(config)
{
    HostName = "irc.cncirc.net",
    UseSsl = true,
    Port = 9999
};

client.EnableV3 = true;
client.RequestCapability(IrcV3Capabilities.LabeledResponse);

client.ConnectionEstablishedEvent += (sender, e) => 
{
    Console.WriteLine("Connected to IRC!");
};

client.ChannelMessageReceivedEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        Console.WriteLine($"NOTICE({e.Target}) from {source}: {e.Message}");
    }
    else
    {
        Console.WriteLine($"MESSAGE({e.Target}) from {source}: {e.Message}");
    }
};

client.PrivateMessageReceivedEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        Console.WriteLine($"NOTICE({source}): {e.Message}");
    }
    else
    {
        Console.WriteLine($"MESSAGE({source}): {e.Message}");
    }
};

client.CtcpReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"Received CTCP ({e.Event}) from {e.Source}");
};

client.MotdReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"Received MOTD: Length = {e.Motd.Length}");
};

client.ServerNoticeReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"SNOTICE({e.Source}): {e.Message}");
};

/*client.ServerFeaturesReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"Received RPL_ISUPPORT: {JsonConvert.SerializeObject(e.ServerFeatures)}");
};*/

client.ErrorReceivedEvent += (sender, e) =>
{
    Console.WriteLine($"ERROR: {e.Message}");
};

await client.Start();

Console.WriteLine("Press <CTRL+C> to cancel...");
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Terminating...");
    e.Cancel = true;
    client.Stop("Exiting...").Wait();
};
