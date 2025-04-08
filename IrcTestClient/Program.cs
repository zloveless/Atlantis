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
    Port = 9999,
    EnableV3 = true
};

client.ConnectionEstablishedEvent += (sender, e) => 
{
    logger.LogInformation("Connected to IRC!");
    client.Send("JOIN #genesis");
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
    else
    {
        logger.LogInformation($"MESSAGE({e.Target}, {e.Tags?.Count ?? 0}) from {source}: {e.Message}");
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

client.ErrorReceivedEvent += (sender, e) =>
{
    logger.LogError($"ERROR: {e.Message}");
};

await client.Start();

Console.WriteLine("Press <CTRL+C> to cancel...");
Console.CancelKeyPress += (sender, e) =>
{
    logger.LogInformation("Terminating...");
    e.Cancel = true;
    client.Stop("Exiting...").Wait();
};

await Log.CloseAndFlushAsync();