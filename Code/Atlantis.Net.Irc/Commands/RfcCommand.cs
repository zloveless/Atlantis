// -----------------------------------------------------------------------------
//  <copyright file="RfcCommand.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc.Commands
{
    using System;
    using System.Linq;

    public abstract class RfcCommand : IRfcCommand
    {
        public IrcConnection Connection { get; internal set; }

        #region Implementation of IRfcCommand

        /// <summary>
        ///     Handles this command using the specified parameters.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        public abstract void Execute(string source, string[] parameters);

        #endregion
    }

    public class PingCommand : RfcCommand
    {
        #region Overrides of RfcCommand

        /// <summary>
        ///     Handles this command using the specified parameters.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        public override void Execute(string source, string[] parameters)
        {
            if (Connection != null)
            {
                var randomString = RandomString(10);

                Connection.SendImmediately("PONG :{0}", randomString);
            }
        }

        #endregion

        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklnopqrstuvwxyz0123456789";
        private static Random rng = new Random();

        private static string RandomString(int count)
        {
            // http://stackoverflow.com/a/1344242/63609

            return new string(Enumerable.Repeat(Characters, count).Select(s => s[rng.Next(s.Length)]).ToArray());
        }
    }
}
