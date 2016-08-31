// -----------------------------------------------------------------------------
//  <copyright file="RenegadeLogParser.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;

    public class RenegadeLogParser : ServerLogParser, IRenegadeParser
    {
        public RenegadeLogParser(IServerConnection serverConnection) : base(serverConnection)
        {
        }

        #region Overrides of ServerLogParser

        protected override void OnLog(object sender, LogMessageReceivedEventArgs args)
        {
            if (args.Message.Length < 3) return;

            int num;
            if (int.TryParse(args.Message.Substring(0, 3), out num))
            {
                var message = args.Message.Substring(3);

                // num => 0 = SSGM, 1 = GameLog, 2 = RenLog, 3 = Console
            }
        }

        #endregion

        #region Implementation of IRenegadeParser

        public event EventHandler<SsgmLogEventArgs> SsgmLog;
        public event EventHandler<RenLogEventArgs> RenegadeLog;
        public event EventHandler<RenLogEventArgs> GameLog;
        public event EventHandler<RenLogEventArgs> ConsoleLog;

        #endregion
    }
}
