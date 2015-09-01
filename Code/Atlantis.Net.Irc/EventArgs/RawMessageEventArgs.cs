namespace Atlantis.Net.Irc
{
    using System;

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
}