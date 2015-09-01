namespace Atlantis.Net.Irc
{
    using System;

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
}