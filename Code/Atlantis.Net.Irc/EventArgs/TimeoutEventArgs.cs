namespace Atlantis.Net.Irc
{
    using System;

    public class TimeoutEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }
}