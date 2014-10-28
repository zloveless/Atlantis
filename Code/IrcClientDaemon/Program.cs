// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace IrcClientDaemon
{
    using System;
    using System.Text;
    using System.Threading;
    using Atlantis.Net.Irc;

    public class Program
    {
        public static void Main(string[] args)
        {
            IrcClient client = new IrcClient
                               {
                                   HostName = "irc.cncfps.com",
                                   Port = 6667,
                                   Nick = "AtlantisTest",
                                   Encoding = Encoding.UTF8,
                               };

            client.ConnectionEstablishedEvent += (s, e) =>
                                                 {
                                                     Console.WriteLine("Connected!");
                                                     return;
                                                 };

            // ReSharper disable once CSharpWarnings::CS4014
            client.Start();

            /*Console.CancelKeyPress += (s, e) => client.Stop();
            while (true)
            {
                new EventWaitHandle(false, EventResetMode.ManualReset).WaitOne(1000);
            }*/

            Console.ReadLine();

            client.Stop();
        }
    }
}
