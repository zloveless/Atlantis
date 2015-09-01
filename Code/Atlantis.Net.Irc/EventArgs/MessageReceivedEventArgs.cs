namespace Atlantis.Net.Irc
{
    using System;

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
}