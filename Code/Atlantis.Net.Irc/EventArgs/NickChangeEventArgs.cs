namespace Atlantis.Net.Irc
{
    using System;

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
}