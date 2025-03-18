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
    EnableV3 = true,
    StrictNames = true
};

client.ConnectionEstablishedEvent += (sender, e) => 
{
    logger.LogInformation("Connected to IRC!");
    client.Send("JOIN #neopub");
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

await Log.CloseAndFlushAsync();