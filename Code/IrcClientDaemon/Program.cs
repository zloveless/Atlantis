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
								   Ident = "atlantis",
	                               Encoding = Encoding.UTF8,
	                               //EnableV3 = true,
                               };

	        client.ConnectionEstablishedEvent += (s, e) =>
	                                             {
		                                             Console.WriteLine("Connected!");

		                                             //((IrcClient)s).Send("JOIN #genesis2001");
		                                             //((IrcClient)s).Send("JOIN #UnifiedTech");
		                                             //((IrcClient)s).Send("PRIVMSG #UnifiedTech :Hello World!");
	                                             };

			/*
	        client.JoinEvent += (s, e) => Console.WriteLine("[JOIN] {0} joined channel {1}", e.Nick, e.Channel);
	        client.PartEvent += (s, e) => Console.WriteLine("[PART] {0} left channel {1} with message \"{2}\"", e.Nick, e.Channel, e.Message ?? "No message");
	        client.NoticeReceivedEvent +=
		        (s, e) => Console.WriteLine("[NOTICE] Received a notice from {0} ({2}) with: {1}",
			        e.Source,
			        e.Message,
			        e.Target == client.Nick || e.Target == null ? "to me" : e.Target); */
			
            // ReSharper disable once CSharpWarnings::CS4014
            client.Start();

            Console.CancelKeyPress += (s, e) => client.Stop("Goodbye!");
            while (true)
            {
                new EventWaitHandle(false, EventResetMode.ManualReset).WaitOne(1000);
            }
        }
    }
}
