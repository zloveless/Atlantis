// -----------------------------------------------------------------------------
//  <copyright file="IrcClientModeParser.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class IrcClientModeParser : IModesStringParser
    {
        private readonly IrcClient _client;

        public IrcClientModeParser(IrcClient client)
        {
            _client = client;
        }

        #region Implementation of IModesStringParser

        public IEnumerable<GenericMode> GetModeList(string modeString, params string[] parameters)
        {
            bool set = false;
            for (int modeIndex = 0, parameterIndex = 0; modeIndex < modeString.Length; ++modeIndex)
            {
                if (modeString[modeIndex] == '+')
                {
                    set = true;
                }
                else if (modeString[modeIndex] == '-')
                {
                    set = false;
                }
                else if (_client.ServerInfo.ListModes.Contains(modeString[modeIndex]))
                { // List modes always require a parameter.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;
                    yield return new GenericMode { Mode = modeString[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.LIST };
                }
                else if (_client.ServerInfo.ModesWithParameter.Contains(modeString[modeIndex]))
                { // Modes that always take a parameter, regardless.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;
                    yield return
                        new GenericMode { Mode = modeString[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.SETUNSET };
                }
                else if (_client.ServerInfo.ModesWithParameterWhenSet.Contains(modeString[modeIndex]))
                { // Modes that only take a parameter when being set.
                    String arg = null;
                    if (set)
                    {
                        arg = parameters[parameterIndex];
                        parameterIndex++;
                    }

                    yield return new GenericMode { Mode = modeString[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.SET };
                }
                else if (_client.ServerInfo.ModesWithNoParameter.Contains(modeString[modeIndex]))
                { // Modes that never take a parameter.
                    yield return new GenericMode { Mode = modeString[modeIndex], IsSet = set, Type = ModeType.NOPARAM };
                }
                else if (_client.ServerInfo.PrefixModes.Contains(modeString[modeIndex]))
                { // Modes that indicate access on a channel.
                    String arg = parameters[parameterIndex];
                    parameterIndex++;

                    yield return
                        new GenericMode { Mode = modeString[modeIndex], IsSet = set, Parameter = arg, Type = ModeType.ACCESS };
                }
            }
        }

        #endregion
    }
}
