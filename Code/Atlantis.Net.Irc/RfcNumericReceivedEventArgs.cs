// -----------------------------------------------------------------------------
//  <copyright file="RfcNumericReceivedEventArgs.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
	using System;

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
}
