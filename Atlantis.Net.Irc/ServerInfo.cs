// -----------------------------------------------------------------------------
//  <copyright file="ServerInfo.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------
namespace Atlantis.Net.Irc
{
	using System;

	public partial class IrcClient
	{
		#region Nested type: ServerInfo

		internal class ServerInfo
		{
			/// <summary>
			/// Represents a value indicating what the maximum length the name of a channel can be on the IRC server.
			/// </summary>
			public int ChannelLength { get; set; }

			/// <summary>
			/// Represents a value indicating what types of channels are allowed (channel prefixes)
			/// </summary>
			public String[] ChanTypes { get; set; }

			/// <summary>
			/// Represents the maximum length of a kick comment.
			/// </summary>
			public int KickLength { get; set; }

			/// <summary>
			/// Represents a char array of available list modes on the IRC server.
			/// </summary>
			public String ListModes { get; set; }

			/// <summary>
			/// Represents the maximum number of entries that can be set per mode.
			/// </summary>
			public int MaxList { get; set; }

			/// <summary>
			/// Represents the maximum number of modes allowed to be set per command.
			/// </summary>
			public int MaxModes { get; set; }

			/// <summary>
			/// Represents a char array of modes that can be un/set on a channel that do not take a parameter (ever).
			/// </summary>
			public String ModesWithNoParameter { get; set; }

			/// <summary>
			/// Represents a char array of modes that can be un/set on a channel that always take a parameter.
			/// </summary>
			public String ModesWithParameter { get; set; }

			/// <summary>
			/// Represents a char array of modes that only take a parameter when being set, no parameter is necessary for unsetting these modes.
			/// </summary>
			public String ModesWithParameterWhenSet { get; set; }

			/// <summary>
			/// Represents the list of prefixes available on the server.
			/// </summary>
			public String Prefixes { get; set; }

			/// <summary>
			/// Represents the list of prefix modes associated with each prefix on the server.
			/// </summary>
			public String PrefixModes { get; set; }

			/// <summary>
			/// Represents the maximum length for a channel topic.
			/// </summary>
			public int TopicLength { get; set; }
		}

		#endregion
	}
}
