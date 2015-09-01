namespace Atlantis.Net.Irc
{
    using System;

    public class CancelableEventArgs : EventArgs
    {
        public bool IsCancelled { get; set; }
    }
}