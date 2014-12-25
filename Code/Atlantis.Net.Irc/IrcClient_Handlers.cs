// -----------------------------------------------------------------------------
//  <copyright file="IrcClient_Handlers.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Channels;
    using System.Text;
    using System.Text.RegularExpressions;
    using Atlantis.Linq;
    using Linq;

    public partial class IrcClient
    {
        #region Fields

        internal ServerInfo info = new ServerInfo();
        private bool useExtendedNames;
        private bool useUserhostNames;
        private DateTime lastMessage;
        private String accessRegex;

        #endregion

        #region Properties

        public char CommandPrefix { get; set; }

        public bool EnableCommandParsing { get; set; }

        public String PrefixModes
        {
            get { return info.PrefixModes; }
        }

        public String Prefixes
        {
            get { return info.Prefixes; }
        }

        public bool StrictNames { get; set; }

        #endregion

        protected IEnumerable<GenericMode> ParseChanModes(String modestr, params String[] parameters)
        {
            bool set = false;
            for (int modeIndex = 0, parameterIndex = 0; modeIndex < modestr.Length; ++modeIndex)
            {
                if (modestr[modeIndex] == '+') set = true;
                else if (modestr[modeIndex] == '-') set = false;
                else if (info.ListModes.Contains(modestr[modeIndex]))
                { // List modes always require a parameter.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;
                    yield return new GenericMode { Mode = modestr[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.LIST };
                }
                else if (info.ModesWithParameter.Contains(modestr[modeIndex]))
                { // Modes that always take a parameter, regardless.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;
                    yield return
                        new GenericMode { Mode = modestr[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.SETUNSET };
                }
                else if (info.ModesWithParameterWhenSet.Contains(modestr[modeIndex]))
                { // Modes that only take a parameter when being set.
                    String arg = null;
                    if (set)
                    {
                        arg = parameters[parameterIndex];
                        parameterIndex++;
                    }

                    yield return new GenericMode { Mode = modestr[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.SET };
                }
                else if (info.ModesWithNoParameter.Contains(modestr[modeIndex]))
                { // Modes that never take a parameter.
                    yield return new GenericMode { Mode = modestr[modeIndex], IsSet = set, Type = ModeType.NOPARAM };
                }
                else if (info.PrefixModes.Contains(modestr[modeIndex]))
                { // Modes that indicate access on a channel.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;

                    yield return
                        new GenericMode { Mode = modestr[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.ACCESS };
                }
            }
        }

        protected IEnumerable<GenericMode> ParseUserModes(String modestr, params String[] parameters)
        {
            bool set = false;
            foreach (char t in modestr)
            {
                switch (t)
                {
                    case '+': set = true; break;
                    case '-': set = false; break;
                    default:
                        yield return new GenericMode { Mode = t, IsSet = set, Type = ModeType.USER };
                        break;
                }
            }
        }

        private async void FillChannelLists(String target)
        {
            foreach (char c in info.ListModes)
            {
                await Send("MODE {0} +{1}", target, c);
            }
        }

        #region Events Handlers

        protected virtual void OnDataRecv(string line)
        {
            lastMessage = DateTime.Now;

            var tokens = line.Split(' ');
            var tokenIndex = 0;

            String source = null;
            if (tokens[tokenIndex][0] == ':')
            {
                // TODO: source parsing
                source = tokens[tokenIndex].Substring(1);
                tokenIndex++;
            }

            if (tokenIndex == tokens.Length)
            {
                // Reached the end.
                // TODO: maybe disconnect? Idk.
                return;
            }

            var commandName = tokens[tokenIndex++];
            var parameters = new List<String>();

            while (tokenIndex != tokens.Length)
            {
                if (tokens[tokenIndex][0] != ':')
                {
                    parameters.Add(tokens[tokenIndex++]);
                    continue;
                }

                parameters.Add(String.Join(" ", tokens.Skip(tokenIndex)).Substring(1));
                break;
            }

            int numeric = 0;
            if (Int32.TryParse(commandName, out numeric))
            {
                OnRfcNumeric(numeric, source, parameters.ToArray());
            }
            else
            {
                OnRfcEvent(commandName, source, parameters.ToArray());
            }
        }

        protected virtual async void OnJoin(String source, String target)
        {
            String nick = source.GetNickFromSource();
            bool me = _currentNick.EqualsIgnoreCase(nick);

            if (me && FillListsOnJoin)
            {
                FillChannelLists(target);
            }

            if (StrictNames || me)
            {
                await Send("NAMES {0}", target);
            }

            var c = GetChannel(target);
            if (!c.HasUser(nick))
            {
                c.AddOrUpdateUser(nick);
            }

            JoinEvent.Raise(this, new JoinPartEventArgs(nick, target, me: me));
        }

        protected virtual void OnModeChanged(char mode, String parameter, String setter, String target, ModeType type)
        {
            ModeChangedEvent.Raise(this, new ModeChangedEventArgs(mode, parameter, setter, target, type));
        }

        protected virtual void OnNickChanged(String source, String currentNick)
        {
            String previousNick = source.GetNickFromSource();

            if (_currentNick.EqualsIgnoreCase(previousNick))
            { // our nick has updated.
                _currentNick = currentNick;
            }

            foreach (var item in _channels.Select(x => x.Value))
            {
                item.ChangeNick(previousNick, currentNick);
            }
        }

        protected virtual void OnNotice(String source, String target, String message)
        {
            NoticeReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));
        }

        protected virtual async void OnPart(String source, String target, String message)
        {
            String sourceNick = source.GetNickFromSource();
            bool me = _currentNick.EqualsIgnoreCase(sourceNick);

            if (me)
            {
                RemoveChannel(target);
            }
            else
            {
                var c = GetChannel(target);
                c.RemoveUser(sourceNick);
            }

            if (StrictNames && !me)
            {
                await Send("NAMES {0}", target);
            }

            PartEvent.Raise(this, new JoinPartEventArgs(sourceNick, target, message, me));
        }

        private void OnPreModeParse(String source, String target, String modestr, String[] parameters)
        {
            // TODO: refactor method name since this isn't going to be a hook-point for overrides
            // TODO: decipher what past-me meant by the above statement.
            // TODO: leave these funny comments for posterity.
            String sourceNick = source.GetNickFromSource();

            if (target.StartsWith("#"))
            {
                var c = GetChannel(target);

                var modes = ParseChanModes(modestr, parameters);
                foreach (var item in modes)
                {
                    if (item.Type == ModeType.LIST)
                    {
                        if (item.IsSet)
                        {
                            c.ListModes.Add(item.Mode, item.Parameter, source);
                        }
                        else
                        {
                            c.ListModes.Remove(item.Mode, item.Parameter);
                        }
                    }
                    else if (item.Type == ModeType.SETUNSET || item.Type == ModeType.SET || item.Type == ModeType.NOPARAM)
                    {
                        if (item.IsSet)
                        {
                            c.Modes.Add(item.Mode, item.Parameter);
                        }
                        else
                        {
                            c.Modes.Remove(item.Mode);
                        }
                    }
                    else if (item.Type == ModeType.ACCESS)
                    {
                        int prefixIndex = info.PrefixModes.IndexOf(item.Mode);
                        char prefix = info.Prefixes[prefixIndex];

                        if (item.IsSet)
                        {
                            c.AddPrefix(item.Parameter, prefix);
                        }
                        else
                        {
                            c.RemovePrefix(item.Parameter, prefix);
                        }
                    }

                    OnModeChanged(item.Mode, item.Parameter, source, target, item.Type);
                }
            }
            else
            { // User mode.
                Debug.WriteLine("[MODE] Received usermode(s) {0} (Source: {1} - Target: {2})", modestr, source, target);

                var modes = ParseUserModes(modestr, parameters); // I don't think parameters are available for usermodes.
                foreach (var item in modes)
                {
                    if (item.IsSet)
                    {
                        Modes.Add(item.Mode);
                    }
                    else
                    {
                        Modes.Remove(item.Mode);
                    }

                    OnModeChanged(item.Mode, null, source, target, item.Type);
                }
            }
        }

        protected virtual async void OnPreRegister()
        {
            if (!Connected)
            {
                return;
            }

            if (!String.IsNullOrEmpty(Password))
            {
                await Send("PASS {0}", Password);
            }

            SetNick(Nick);
            await Send("USER {0} 0 * {1}", Ident, RealName.Contains(" ") ? String.Concat(":", RealName) : RealName);

            /* This causes a registration timeout.
            if (EnableV3)
            {
                await Task.Delay(500);
                await Send("CAP LS"); // Request capabilities from the IRC server.
            }*/
        }

        protected virtual void OnPrivmsg(String source, String target, String message)
        {
            PrivmsgReceivedEvent.Raise(this, new MessageReceivedEventArgs(source, target, message));

            // TODO: Support non-channel commands (ie: PM commands)
            // TODO: Support non-standard channels (ie: use CHANTYPES from 005).
            if (EnableCommandParsing && target[0].Equals('#') && message.Length > 1 && message[0] == CommandPrefix)
            {
                string[] toks       = message.Split(' ');
                string command      = toks[0].Substring(1);
                string[] parameters = toks.Skip(1).ToArray();

                char access = '\0';
                var c = GetChannel(target);
                if (c != null)
                {
                    string nick = source.GetNickFromSource();
                    PrefixList l;
                    if (c.Users.TryGetValue(nick, out l))
                    {
                        access = l.HighestPrefix;
                    }
                }

                CanExecuteCommandEventArgs args = new CanExecuteCommandEventArgs(command, source, access, parameters);
                CanExecuteCommandEvent.Raise(this, args);

                if (args.CanExecute)
                {
                    CommandExecutedEvent.Raise(this, new CommandExecuteEventArgs(command, source, access, parameters));
                }
            }
        }

        protected virtual void OnQuit(String source, String message)
        {
            string sourceNick = source.GetNickFromSource();

            lock (_channels)
            {
                foreach (var c in _channels.Values.Where(x => x.HasUser(sourceNick)))
                {
                    c.RemoveUser(sourceNick);
                }
            }

            QuitEvent.Raise(this, new QuitEventArgs(source, message));
        }

        protected virtual async void OnRfcEvent(String command, String source, String[] parameters)
        {
            if (command.EqualsIgnoreCase("PING"))
            {
                await Send("PONG {0}", parameters[0]);
            }
            else if (command.EqualsIgnoreCase("CAP"))
            { // :wolverine.de.cncfps.com CAP 574AAACA9 LS :away-notify extended-join account-notify multi-prefix sasl tls userhost-in-names
                if (!EnableV3)
                {  // Ignore it. ircv3 spec not enabled.
                    return;
                }

                var caps = new StringBuilder();
                if (parameters.Any(x => x.EqualsIgnoreCase("multi-prefix")))
                {
                    caps.Append("multi-prefix ");
                    useExtendedNames = true;
                }
                else if (parameters.Any(x => x.EqualsIgnoreCase("userhost-in-names")))
                {
                    caps.Append("userhost-in-names ");
                    useUserhostNames = true;
                }

                if (caps.Length > 0)
                {
                    await Send("CAP REQ :{0}", caps.ToString().Trim(' '));
                }
            }
            else if (command.EqualsIgnoreCase("PRIVMSG"))
            {
                if (parameters.Length < 2)
                {
                    Debug.WriteLine("Privmsg received with less than 2 parameters.");
                }
                else
                {
                    if (source.IsServerSource())
                    {
                        OnPrivmsg(source, null, String.Join(" ", parameters));
                    }
                    else
                    {
                        OnPrivmsg(source, parameters[0], String.Join(" ", parameters.Skip(1)));
                    }
                }
            }
            else if (command.EqualsIgnoreCase("NOTICE"))
            {
                if (parameters.Length < 2)
                {
                    Debug.WriteLine("Notice received with less than 2 parameters.");
                }
                else
                {
                    if (source.IsServerSource())
                    {
                        OnNotice(source, null, String.Join(" ", parameters));
                    }
                    else
                    {
                        OnNotice(source, parameters[0], String.Join(" ", parameters.Skip(1)));
                    }
                }
            }
            else if (command.EqualsIgnoreCase("JOIN"))
            {
                Debug.Assert(source != null, "Source is null on join.");

                OnJoin(source, parameters[0]);
            }
            else if (command.EqualsIgnoreCase("PART"))
            {
                Debug.Assert(source != null, "Source is null on part.");

                String message = null;
                if (parameters.Length > 1)
                {
                    message = String.Join(" ", parameters.Skip(1));
                }

                OnPart(source, parameters[0], message);
            }
            else if (command.EqualsIgnoreCase("MODE"))
            {
                OnPreModeParse(source, parameters[0], parameters[1], parameters.Skip(2).ToArray());
            }
            else if (command.EqualsIgnoreCase("QUIT"))
            {
                OnQuit(source, String.Join(" ", parameters));
            }
            else if (command.EqualsIgnoreCase("NICK"))
            {
                OnNickChanged(source, parameters[0]);
            }
            else
            {
                Debug.WriteLine("[{0}] Received command from {1} with {2} parameters: {{{3}}}",
                    command,
                    source,
                    parameters.Length,
                    parameters.JoinFormat(", ", @"""{0}"""));
            }
        }

        protected virtual async void OnRfcNumeric(Int32 numeric, String source, String[] parameters)
        {
            RfcNumericReceivedEvent.Raise(this, new RfcNumericReceivedEventArgs(numeric, String.Join(" ", parameters)));

            if (numeric == RPL_WELCOME)
            { // Welcome packet
                connectingLock.Release();
                ConnectionEstablishedEvent.Raise(this, EventArgs.Empty);
            }
            else if (numeric == 5)
            { // Contribution for handy parsing of 005 courtesy of @aca20031
                Dictionary<String, String> args = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                String[] tokens = parameters.Skip(1).ToArray();
                foreach (String token in tokens)
                {
                    int equalIndex = token.IndexOf('=');
                    if (equalIndex >= 0)
                    {
                        args[token.Substring(0, equalIndex)] = token.Substring(equalIndex + 1);
                    }
                    else
                    {
                        args[token] = "";
                    }
                }

                if (args.ContainsKey("PREFIX"))
                {
                    String value = args["PREFIX"];
                    Match m;
                    if (value.TryMatch(@"\(([^\)]+)\)(\S+)", out m))
                    {
                        info.PrefixModes = m.Groups[1].Value;
                        info.Prefixes = m.Groups[2].Value;
                    }
                }

                if (args.ContainsKey("CHANMODES"))
                {
                    String[] chanmodes = args["CHANMODES"].Split(',');

                    info.ListModes = chanmodes[0];
                    info.ModesWithParameter = chanmodes[1];
                    info.ModesWithParameterWhenSet = chanmodes[2];
                    info.ModesWithNoParameter = chanmodes[3];
                }

                if (args.ContainsKey("MODES"))
                {
                    int modeslen;
                    if (Int32.TryParse(args["MODES"], out modeslen))
                    {
                        info.MaxModes = modeslen;
                    }
                }

                if (args.ContainsKey("TOPICLEN"))
                {
                    int topiclen;
                    if (Int32.TryParse(args["TOPICLEN"], out topiclen))
                    {
                        info.TopicLength = topiclen;
                    }
                }

                if (!EnableV3)
                {
                    if (args.ContainsKey("NAMESX"))
                    {	// Request the server send us extended NAMES (353)
                        // This will format a RPL_NAMES using every single prefix the user has on a channel.

                        useExtendedNames = true;
                        await Send("PROTOCTL NAMESX");
                    }

                    if (args.ContainsKey("UHNAMES"))
                    {
                        useUserhostNames = true;
                        await Send("PROTOCTL UHNAMES");
                    }
                }
            }
            else if (numeric == 353)
            {	// note: NAMESX and UHNAMES are not mutually exclusive.
                // NAMESX: (?<prefix>[!~&@%+]*)(?<nick>[^ ]+)
                // UHNAMES: (?<prefix>[!~&@%+]*)(?<nick>[^!]+)!(?<ident>[^@]+)@(?<host>[^ ]+)
                // (?<prefix>[!~&@%+]*)(?<nick>[^!]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?

                if (String.IsNullOrEmpty(accessRegex))
                {
                    accessRegex = String.Format(@"(?<prefix>[{0}]*)(?<nick>[^! ]+)(?:!(?<ident>[^@]+)@(?<host>[^ ]+))?", Prefixes);
                }

                var c = GetChannel(parameters[2]); // never null.
                var names = String.Join(" ", parameters.Skip(3));

                MatchCollection matches;
                if (names.TryMatches(accessRegex, out matches))
                {
                    foreach (Match item in matches)
                    { // for now, we only care for the nick and the prefix(es).
                        String nick = item.Groups["nick"].Value;

                        if (useExtendedNames)
                        {
                            c.AddOrUpdateUser(nick, item.Groups["prefix"].Value.ToCharArray());
                        }
                        else
                        {
                            char prefix = item.Groups["prefix"].Value.Length > 0 ? item.Groups["prefix"].Value[0] : (char)0;
                            c.AddOrUpdateUser(nick, new[] { prefix });
                        }

                        //Console.WriteLine("Found \"{0}\" on channel {1}: {2}", nick, c, item.Groups["prefix"].Value);
                    }
                }
            }
            /*else
            {
                Console.WriteLine("[{0:000}] Numeric received with {1} parameters: {{{2}}}",
                    numeric,
                    parameters.Length,
                    String.Join(",", parameters));
            }*/
        }

        #endregion

        #region Callbacks

        protected virtual void WorkerCallback(object state)
        {
            stream = client.GetStream();

            OnPreRegister(); // Send registration data.

            // TODO: Accept a certificate parameter (read: properly) for sending to the IRC server.
            /*if (Options.HasFlag(ConnectOptions.Secure))
            {
                var ssl = new SslStream(stream, true);
                ssl.AuthenticateAsClient("", new X509CertificateCollection(), SslProtocols.Tls12, false);
            }*/

            reader = new StreamReader(stream, Encoding);

            while (Connected)
            {
                if (stream.DataAvailable)
                {
                    while (!reader.EndOfStream)
                    {
                        String line = reader.ReadLine().TrimIfNotNull();
                        if (!String.IsNullOrEmpty(line))
                        {
                            OnDataRecv(line);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
