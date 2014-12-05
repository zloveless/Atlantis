// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace IrcClientDaemon
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Atlantis.Net.Irc;
    using Atlantis.Net.Irc.Linq;

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

	        client.ConnectionEstablishedEvent += async (s, e) =>
	                                             {
		                                             Console.WriteLine("Connected!");

		                                             //((IrcClient)s).Send("JOIN #genesis2001");
		                                             await ((IrcClient)s).Send("JOIN #test");
		                                             //((IrcClient)s).Send("PRIVMSG #UnifiedTech :Hello World!");
	                                             };

	        client.PrivmsgReceivedEvent += async (s, e) =>
	                                       {
		                                       var cl = s as IrcClient;
		                                       Debug.Assert(cl != null);

		                                       if (e.Message.StartsWith("!priv") && e.IsChannel)
		                                       {
                                                   String nick = e.Source.GetNickFromSource();

			                                       var c = cl.GetChannel(e.Target);
			                                       PrefixList l;
			                                       c.Users.TryGetValue(nick, out l);
			                                       Debug.Assert(l != null,
				                                       String.Format("Null prefix list found for user: {0}", e.Source));

			                                       await cl.Send(
			                                               "PRIVMSG {0} :{1}, I see you have the following prefixes: {2} (Highest prefix: {3})",
				                                       e.Target,
				                                       nick,
				                                       l,
				                                       l.HighestPrefix);
		                                       }
                                               else if (e.Message.StartsWith("!test") && e.IsChannel)
                                               {
                                                   var buf = new byte[30];
                                                   var rnd = new Random(Guid.NewGuid().GetHashCode());

                                                   for (var i = 0; i < 100; ++i)
                                                   {
                                                       rnd.NextBytes(buf);

                                                       var str = BitConverter.ToString(buf).Replace("-", "");
                                                       
                                                       await ((IrcClient)s).Send("PRIVMSG {0} :{1}", e.Target, str);
                                                   }
                                               }
	                                       };

			/*
	        client.JoinEvent += (s, e) => Console.WriteLine("[JOIN] {0} joined channel {1}", e.Source, e.Channel);
	        client.PartEvent += (s, e) => Console.WriteLine("[PART] {0} left channel {1} with message \"{2}\"", e.Source, e.Channel, e.Message ?? "No message");
	        client.NoticeReceivedEvent +=
		        (s, e) => Console.WriteLine("[NOTICE] Received a notice from {0} ({2}) with: {1}",
			        e.Source,
			        e.Message,
			        e.Target == client.Source || e.Target == null ? "to me" : e.Target); */
			
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
