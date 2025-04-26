namespace Atlantis.Net.Irc;

using Events;
using JetBrains.Annotations;

[PublicAPI]
public static class IrcClientExtensions
{
    /// <summary>
    /// Wraps the specified string with an IRC color code. 
    /// </summary>
    /// <param name="source">The string to wrap with color codes.</param>
    /// <param name="colorCode">The color between 0 and 99.</param>
    /// <returns>The string wrapped with color tokens.</returns>
    /// <exception cref="ArgumentOutOfRangeException">thrown if the specified color code was outside the range of [0..99].</exception>
    public static string Colorize(this string source, int colorCode) 
    {
        // ReSharper disable once MergeIntoLogicalPattern
        if (colorCode < 0 || colorCode > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCode));
        }

        return $"\x03{colorCode}{source}\x03";
    }
    
    /// <summary>
    /// Replies to the specified target using the IRC client-to-client protocol reply scheme.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="ctcpEvent">The CTCP event type we're targeting.</param>
    /// <param name="target">The target of the CTCP reply.</param>
    /// <param name="message">The response message for the CTCP event.</param>
    /// <exception cref="NotSupportedException">thrown if trying to send an action message.</exception>
    /// <exception cref="InvalidOperationException">thrown if trying to send a reply to a channel.</exception>
    public static void CtcpReply(this IrcClient client, CtcpEvent ctcpEvent, string target, string message)
    {
        if (ctcpEvent == CtcpEvent.Action)
        {
            throw new NotSupportedException("Actions cannot be the target of a ctcp reply.");
        }
        
        if (client.IsChannelName(target))
        {
            throw new InvalidOperationException("Client-to-client protocol replies never target channels.");
        }
        
        client.Send($"NOTICE {target} :\x01{ctcpEvent.ToString().ToUpper()} {message}\x01");
    }

    /// <summary>
    /// Replies to the specified target using the IRC client-to-client protocol reply scheme.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="ctcpEvent">The CTCP event type we're targeting.</param>
    /// <param name="target">The target of the CTCP reply.</param>
    /// <param name="message">The response message for the CTCP event.</param>
    /// <exception cref="NotSupportedException">thrown if trying to send an action message.</exception>
    /// <exception cref="InvalidOperationException">thrown if trying to send a reply to a channel.</exception>
    public static async Task CtcpReplyAsync(this IrcClient client, CtcpEvent ctcpEvent, string target, string message) 
    {
        if (ctcpEvent == CtcpEvent.Action)
        {
            throw new NotSupportedException("Actions cannot be the target of a ctcp reply.");
        }
        
        if (client.IsChannelName(target))
        {
            throw new InvalidOperationException("Client-to-client protocol replies never target channels.");
        }
        
        await client.SendAsync($"NOTICE {target} :\x01{ctcpEvent.ToString().ToUpper()} {message}\x01");
    }
    
    /// <summary>
    /// Attempts to join the specified channel. 
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="channelName">the channel name to try to join.</param>
    /// <param name="password">If the channel requires a key to enter, specify it here.</param>
    /// <exception cref="ArgumentException">Thrown if the channel name is not an allowable channel on the <see cref="IrcClient"/>.</exception>
    public static void JoinChannel(this IrcClient client, string channelName, string? password = null) 
    {
        if (!client.IsChannelName(channelName))
        {
            throw new ArgumentException($"To join a channel, you must provide a valid channel target. '{channelName}' is not valid.", nameof(channelName));
        }

        if (client.IsChannel(channelName)) return;
        
        var includedPassword = password != null ? $" :{password}" : string.Empty;
        client.Send($"JOIN {channelName}{includedPassword}");
    }
    
    /// <summary>
    /// Attempts to join the specified channel. 
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="channelName">the channel name to try to join.</param>
    /// <param name="password">If the channel requires a key to enter, specify it here.</param>
    /// <exception cref="ArgumentException">Thrown if the channel name is not an allowable channel on the <see cref="IrcClient"/>.</exception>
    public static async Task JoinChannelAsync(this IrcClient client, string channelName, string? password) 
    {
        if (!client.IsChannelName(channelName))
        {
            throw new ArgumentException($"To join a channel, you must provide a valid channel target. '{channelName}' is not valid.", nameof(channelName));
        }

        if (client.IsChannel(channelName)) return;
        
        var includedPassword = password != null ? $" :{password}" : string.Empty;
        await client.SendAsync($"JOIN {channelName}{includedPassword}");
    }

    /// <summary>
    ///     Sends the specified target a message.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="target"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentException">thrown if the target is a channel and the client is not on the channel.</exception>
    public static void Message(this IrcClient client, string target, string message)
    {
        if (client.IsChannelName(target) && !client.IsChannel(target))
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }

        client.Send($"PRIVMSG {target} :{message}");
    }

    /// <summary>
    ///     Sends the specified target a message.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="target">The target of the message. Can be a channel or user.</param>
    /// <param name="message">The message to send.</param>
    /// <exception cref="ArgumentException">thrown if the target is a channel and the client is not on the channel.</exception>
    public static async Task MessageAsync(this IrcClient client, string target, string message)
    {
        if (client.IsChannelName(target) && !client.IsChannel(target))
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }

        await client.SendAsync($"PRIVMSG {target} :{message}");
    }
    
    /// <summary>
    /// Sends the specified target a message as a notice.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="target">The target of the message. Can be a channel or user.</param>
    /// <param name="message">The message to send.</param>
    /// <exception cref="ArgumentException">thrown if the target is a channel and the client is not on the channel.</exception>
    /// <exception cref="InvalidOperationException">thrown if the client does not support sending notices to channels.</exception>
    public static void Notice(this IrcClient client, string target, string message) 
    {
        if (client.IsChannelName(target) && !client.IsChannel(target)) 
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }
        
        if (client.IsChannelName(target) && !client.AllowNoticeChannels)
        {
            throw new InvalidOperationException($"The client does not allow sending notices to channels. Please check {nameof(client.AllowNoticeChannels)} if this is an error.");
        }

        client.Send($"NOTICE {target} :{message}");
    }
    
    /// <summary>
    /// Sends the specified target a message as a notice.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="target">The target of the message. Can be a channel or user.</param>
    /// <param name="message">The message to send.</param>
    /// <exception cref="ArgumentException">thrown if the target is a channel and the client is not on the channel.</exception>
    /// <exception cref="InvalidOperationException">thrown if the client does not support sending notices to channels.</exception>
    public static async Task NoticeAsync(this IrcClient client, string target, string message) 
    {
        if (client.IsChannelName(target) && !client.IsChannel(target)) 
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }
        
        if (client.IsChannelName(target) && !client.AllowNoticeChannels)
        {
            throw new InvalidOperationException($"The client does not allow sending notices to channels. Please check {nameof(client.AllowNoticeChannels)} if this is an error.");
        }

        await client.SendAsync($"NOTICE {target} :{message}");
    }
    
    /// <summary>
    /// Attempts to part the specified channel.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="channelName">The channel name to leave</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the channel does not exist on the <see cref="IrcClient"/>.</exception>
    public static void PartChannel(this IrcClient client, string channelName) 
    {
        if (!client.IsChannel(channelName))
        {
            throw new ArgumentOutOfRangeException(nameof(channelName), "The specified channel does not exist on the IrcClient's internal tracking.");
        }

        client.Send($"PART {channelName}");
    }
    
    /// <summary>
    /// Attempts to part the specified channel.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="channelName">The channel name to leave</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the channel does not exist on the <see cref="IrcClient"/>.</exception>
    public static async Task PartChannelAsync(this IrcClient client, string channelName) 
    {
        if (!client.IsChannel(channelName))
        {
            throw new ArgumentOutOfRangeException(nameof(channelName), "The specified channel does not exist on the IrcClient's internal tracking.");
        }

        await client.SendAsync($"PART {channelName}");
    }
    
    /// <summary>
    /// Sends the specified client-to-client protocol request to the specified target.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="ctcpEvent">The CTCP event type we're targeting.</param>
    /// <param name="target">The target of the CTCP reply.</param>
    /// <param name="parameter">The request parameter for the CTCP event.</param>
    /// <exception cref="NotSupportedException">thrown if trying to send an action as a request.</exception>
    /// <exception cref="ArgumentException">thrown if trying to send a request to a channel the client is not on.</exception>
    public static void SendCtcp(this IrcClient client, CtcpEvent ctcpEvent, string target, string? parameter = null)
    {
        if (ctcpEvent == CtcpEvent.Action)
        {
            throw new NotSupportedException("Actions are not a valid client-to-client protocol event.");
        }
        
        if (client.IsChannelName(target) && !client.IsChannel(target)) 
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }

        if (ctcpEvent == CtcpEvent.Ping && parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter), "When sending a ping request, you need to specify a parameter of the current timestamp.");
        }
        
        parameter = parameter != null ? $" {parameter}" : string.Empty;
        client.Send($"PRIVMSG {target} :\x01{ctcpEvent.ToString().ToUpper()}{parameter}\x01");
    }
    
    /// <summary>
    /// Sends the specified client-to-client protocol request to the specified target.
    /// </summary>
    /// <param name="client">The client from which to send.</param>
    /// <param name="ctcpEvent">The CTCP event type we're targeting.</param>
    /// <param name="target">The target of the CTCP reply.</param>
    /// <param name="parameter">The request parameter for the CTCP event.</param>
    /// <exception cref="NotSupportedException">thrown if trying to send an action as a request.</exception>
    /// <exception cref="ArgumentException">thrown if trying to send a request to a channel the client is not on.</exception>
    public static async Task SendCtcpAsync(this IrcClient client, CtcpEvent ctcpEvent, string target, string? parameter = null)
    {
        if (ctcpEvent == CtcpEvent.Action)
        {
            throw new NotSupportedException("Actions are not a valid client-to-client protocol event.");
        }
        
        if (client.IsChannelName(target) && !client.IsChannel(target)) 
        {
            throw new ArgumentException($"The channel name '{target}' is either invalid or does not exist.",
                nameof(target));
        }
        
        if (ctcpEvent == CtcpEvent.Ping && parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter), "When sending a ping request, you need to specify a parameter of the current timestamp.");
        }
        
        parameter = parameter != null ? $" {parameter}" : string.Empty;
        
        await client.SendAsync($"PRIVMSG {target} :\x01{ctcpEvent.ToString().ToUpper()}{parameter}\x01");
    }
}
