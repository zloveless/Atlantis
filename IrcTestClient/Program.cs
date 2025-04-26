using Atlantis.Net.Irc;
using Microsoft.Extensions.Logging;
using Serilog;

#region Logging

Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Debug()
             .WriteTo.Console()
             .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<Program>();

#endregion

var config = IrcClientConfiguration.New("GTestClient");
var client = new IrcClient(config, logger)
{
    HostName = "irc.cncirc.net",
    UseSsl = true,
    Port = 6697
};

client.ConnectionEstablishedEvent += (sender, e) => 
{
    logger.LogInformation("Connected to IRC!");
    client.JoinChannel("#genesis");
};

client.CtcpReceivedEvent += (sender, e) =>
{
    if (!e.IsReply) return;

    var tagStr = string.Empty;
    if (e.Tags != null)
    {
        tagStr = string.Join(";", e.Tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
    
    logger.LogInformation($"CTCP REPLY({e.Event}): From={e.Source} Tags: {tagStr}");
};

client.SocketDisconnectEvent += (sender, e) =>
{
    logger.LogError("The client disconnected. Possibly rematurely.");
};

client.KickEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.UserPrefix);
    logger.LogInformation($"*** KICK: {source} removed {e.Target} from {e.Channel} for '{e.Reason}'.");
};

client.TopicChangedEvent += (sender, e) =>
{
    logger.LogInformation($"*** TOPIC CHANGED({e.Channel}): Old='{e.OldTopic}', New='{e.Topic}'");
};

client.ChannelMessageReceivedEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        logger.LogInformation($"NOTICE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
    else if (e.IsAction)
    {
        logger.LogInformation($"ACTION({e.Target}, {e.Tags?.Count ?? 0}): {source} {e.Message}");
    }
    else
    {
        logger.LogInformation($"MESSAGE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
    
    if (!e.IsNotice && e.Message.StartsWith("!hello")) 
    {
        client.Message(e.Target, "Hello world");
    }
    else if (!e.IsNotice && e.Message.StartsWith("!tag"))
    {
        client.Send($"@+aaa;foo=bar;baz PRIVMSG {e.Target} :This message has a client only tag.");
    }
    else if (!e.IsNotice && e.Message.StartsWith("!modes"))
    {
        var modes = client.GetChannelUserModes(e.Target, e.Source);
        client.Message(e.Target, $"Hello {source.Nick}, your mode(s) for {e.Target} are: {modes}");
    }
    else if (!e.IsNotice && e.Message.StartsWith("!version"))
    {
        // TODO: Figure out how to match requests to replies. Possible case for labels/message tags?
        //client.SendCtcp(CtcpEvent.Version, source.Nick, string.Empty);
        client.Send($"@label=foo PRIVMSG {source.Nick} :\x01" + $"VERSION\x01");
    }
    else if (!e.IsNotice && e.Message.StartsWith("!part"))
    {
        logger.LogInformation($"Parting {e.Target}");
        client.PartChannel(e.Target);
    }
};

client.PrivateMessageReceivedEvent += (sender, e) =>
{
    var source = IrcSource.FromPrefix(e.Source);
    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
    if (e.IsNotice)
    {
        logger.LogInformation($"NOTICE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
    else if (e.IsAction)
    {
        logger.LogInformation($"ACTION({e.Target}, {e.Tags?.Count ?? 0}): {source} {e.Message}");
    }
    else
    {
        logger.LogInformation($"MESSAGE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
    }
};

client.ErrorReceivedEvent += (sender, e) =>
{
    logger.LogError($"ERROR: {e.Message}");
};

Console.CancelKeyPress += (sender, e) =>
{
    logger.LogInformation("Terminating...");
    e.Cancel = true;
    client.Stop("Client exiting.");
};

Console.WriteLine("Press <CTRL+C> to cancel...");
try
{
    await client.Start();
}
catch (OperationCanceledException)
{
    // delicious!
}

await Log.CloseAndFlushAsync();
