// -----------------------------------------------------------------------------
//  <copyright file="QuitEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

	public class QuitEventArgs : EventArgs
	{
		public QuitEventArgs(string source, string message)
		{
			Nick = source;
			Message = message;
		}

		public string Nick { get; private set; }

		public string Message { get; private set; }
	}
}
