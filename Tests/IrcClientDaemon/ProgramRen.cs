// -----------------------------------------------------------------------------
//  <copyright file="ProgramRen.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace IrcClientDaemon
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Atlantis.Net.GameServer;

    public class ProgramRen : IDisposable
    {
        private readonly IServerConnection renServer = new RenegadeConnection("", 0000);
        
        public static void Main(string[] args)
        {
            using (var p = new ProgramRen())
            {
                p.Run();
            }
        }

        public void Dispose()
        {
            renServer.Dispose();
        }

        public void Run()
        {
            renServer.Connect();

            Trace.Listeners.Clear();

            var fileLogger = new TextWriterTraceListener(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName.Replace(".exe", ".log")))
                                 {
                                     Name = "TextLogger",
                                     TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
                                 };

            var consoleLogger = new ConsoleTraceListener(false)
                                    {
                                        TraceOutputOptions = TraceOptions.DateTime
                                    };

            Trace.Listeners.Add(fileLogger);
            Trace.Listeners.Add(consoleLogger);
            Trace.AutoFlush = true;

            var events = (IRenegadeEvents)renServer.Parser;

            if (events != null)
            {
                events.RenegadeLog += (s, e) =>
                    {
                        Trace.WriteLine(string.Format("[RENLOG] {0}", e.Message));
                    };
                events.SsgmLog += (s, e) =>
                    {
                        Trace.WriteLine(string.Format("[SSGM] {0} => {1}", e.Header, e.Message));
                    };
                events.GameLog += (s, e) =>
                    {
                        Trace.WriteLine(string.Format("[GAMELOG] {0} => {1}", e.Event, e.Message));
                    };
                events.InvalidLog += (s, e) =>
                    {
                        Trace.WriteLine(string.Format("[UNKNOWN LOG] {0}", e.Message));
                    };
            }

            Console.ReadLine();
            renServer.Disconnect();
        }
    }
}
