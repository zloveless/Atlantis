namespace Atlantis.Net.Irc
{
    using System;

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
}