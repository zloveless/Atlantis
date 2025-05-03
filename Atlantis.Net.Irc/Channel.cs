using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

/// <summary>
///     Represents a channel, including all of its data and users.
/// </summary>
[PublicAPI]
public class Channel
{
    private readonly IrcClient _client;

    public Channel(string channelName, IrcClient client)
    {
        _client = client;
        Name = channelName;
    }

    /// <summary>
    ///     Represents a collection of channel modes applied to a channel.
    /// </summary>
    public HashSet<ChannelMode> Modes { get; } = [];

    /// <summary>
    ///     Represents the channel's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets or sets the topic for the channel.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    ///     Represents a collection of users and their assigned channel modes, if available.
    /// </summary>
    public HashSet<ChannelUser> Users { get; } = [];

    /// <summary>
    ///     Searches for the user.
    /// </summary>
    /// <param name="userNamePrefix"></param>
    /// <returns></returns>
    public ChannelUser? FindUser(string userNamePrefix)
    {
        Func<ChannelUser, bool> searchPredicate =
            cu => cu.User.Equals(userNamePrefix, StringComparison.OrdinalIgnoreCase);
        if (_client.UseUserHostInNames)
        {
            searchPredicate = cu => cu.User.StartsWith(userNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        return Users.FirstOrDefault(searchPredicate);
    }

    /// <summary>
    ///     Attempts to retrieve a user's access modes from the channel.
    /// </summary>
    /// <param name="userNameOrUserPrefix">The nickname of the user.</param>
    /// <param name="channelUser">The resulting pair of a user, their prefix if available, and their channel modes.</param>
    /// <returns>Whether the operation was successful.</returns>
    public bool TryGetUserModes(string userNameOrUserPrefix, [MaybeNullWhen(false)] out ChannelUser channelUser)
    {
        channelUser = FindUser(userNameOrUserPrefix);
        return channelUser != null;
    }

    /// <summary>
    ///     Adds a user with the specified access modes or account name.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="accessModes"></param>
    /// <param name="accountName"></param>
    public void AddOrUpdateUser(string userName, string? accessModes = null, string? accountName = null)
    {
        if (accessModes != null)
        {
            if (TryGetUserModes(userName, out var channelUser))
            {
                Users.Remove(channelUser);
                channelUser = channelUser with { Modes = accessModes };
            }
            else
            {
                channelUser = new ChannelUser(userName, accessModes);
            }

            Users.Add(channelUser);
        }
        
        if (accountName != null)
        {
            var currentUser = FindUser(userName);
            if (currentUser == null) return;
            
            // Check if the account would actually change if we update it.
            // Save an update.
            var isSameAccount = currentUser.AccountName?.Equals(accountName, StringComparison.OrdinalIgnoreCase) ?? false;
            if (isSameAccount) return;

            Users.Remove(currentUser);
            currentUser = currentUser with { AccountName = accountName };
            Users.Add(currentUser);
        }
    }

    /// <summary>
    ///     Removes the specified user from the internal user list.
    /// </summary>
    /// <param name="userName"></param>
    public void RemoveUser(string userName)
    {
        if (TryGetUserModes(userName, out var channelUser)) 
        {
            Users.Remove(channelUser);
        }
    }
}
