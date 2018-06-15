// -----------------------------------------------------------------------------
//  <copyright file="IrcClientEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;
	using System.Text.RegularExpressions;

	#region External type: JoinPartEventArgs

	public class JoinPartEventArgs : EventArgs
	{
		public JoinPartEventArgs(string nick, string channel, String message = null, bool me = false)
		{
			Channel = channel;
			Nick = nick;

			Message = message;
			IsMe = me;
		}

		public bool IsMe { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the name of the user that joined or parted the specified channel.
		/// </summary>
		public string Nick { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the target of the join or part event.
		/// </summary>
		public string Channel { get; private set; }

		/// <summary>
		///		<para>Gets a <see cref="T:System.String" /> value representing the client part message received.</para>
		///		<para>This will always be null on join.</para>
		/// </summary>
		public String Message { get; private set; }
	}

	#endregion

	#region External type: MessageReceivedEventArgs

	public class MessageReceivedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.EventArgs" /> class.
		/// </summary>
		public MessageReceivedEventArgs(string source, string target, string message)
		{
			Source = source;
			Message = message;
			Target = target;

			IsChannel = target.StartsWith("#");
		}

		public bool IsChannel { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Source { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Target { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Message { get; private set; }
	}

	#endregion

	#region External type: NickChangeEventArgs

	public class NickChangeEventArgs : EventArgs
	{
		public NickChangeEventArgs(string oldNick, string newNick)
		{
			OldNick = oldNick;
			NewNick = newNick;
		}

		public string OldNick { get; set; }

		public string NewNick { get; set; }
	}

	#endregion

	#region External type: ProtocolMessageEventArgs

	public class ProtocolMessageEventArgs : EventArgs
	{
		public ProtocolMessageEventArgs(Match match, string message)
		{
			Message = message;
			Match = match;
		}

		/// <summary>
		/// 
		/// </summary>
		public Match Match { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Message { get; private set; }
	}

	#endregion

	#region External type: QuitEventArgs

	public class QuitEventArgs : EventArgs
	{
		public QuitEventArgs(string source, string message)
		{
			Source = source;
			Message = message;
		}

		public string Source { get; private set; }

		public string Message { get; private set; }
	}

	#endregion
	
	#region External type: RfcNumericReceivedEventArgs

	public class RfcNumericReceivedEventArgs : EventArgs
	{
		public RfcNumericReceivedEventArgs(int numeric, string message)
		{
			Numeric = numeric;
			Message = message;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Int32" /> value representing the raw numeric received from the IRC server.
		/// </summary>
		public int Numeric { get; private set; }

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the message after the raw numeric.
		/// </summary>
		public string Message { get; private set; }
	}

	#endregion

	#region External type: RawMessageEventArgs

	public class RawMessageEventArgs : EventArgs
	{
		public RawMessageEventArgs(string message)
		{
			Message = message;
			Tokens = message.Split(' ');
		}

		/// <summary>
		/// Gets a <see cref="T:System.String" /> value representing the message received from the IRC server.
		/// </summary>
		public string Message { get; private set; }
		
		public string[] Tokens { get; private set; }
	}

	#endregion

	#region External type: TimeoutEventArgs

	public class TimeoutEventArgs : EventArgs
	{
		public bool Handled { get; set; }
	}

	#endregion

	#region External class: CancelableEventArgs

	public class CancelableEventArgs : EventArgs
	{
		public bool IsCancelled { get; set; }
	}

	#endregion

	#region External class: HandledEventArgs

	public class HandledEventArgs : EventArgs
	{
		public bool IsHandled { get; set; }
	}

	#endregion

	#region External type: ModeChangedEventArgs

	public class ModeChangedEventArgs : EventArgs
	{
		public ModeChangedEventArgs(char mode, String parameter, String setter, String target, ModeType type)
		{
			Mode = mode;
			Parameter = parameter;
			Target = target;
			Setter = setter;
			Type = type;
		}

		public char Mode { get; private set; }

		public String Parameter { get; private set; }

		public String Setter { get; private set; }

		public String Target { get; private set; }

		public ModeType Type { get; private set; }
	}

	#endregion
}
