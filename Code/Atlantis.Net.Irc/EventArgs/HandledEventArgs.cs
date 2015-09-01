namespace Atlantis.Net.Irc
{
    using System;

    public class HandledEventArgs : EventArgs
    {
        public bool IsHandled { get; set; }
    }
}