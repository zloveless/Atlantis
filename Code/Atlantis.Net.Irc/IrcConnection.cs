// -----------------------------------------------------------------------------
//  <copyright file="IrcConnection.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;

    public class IrcConnection
    {
        private readonly SemaphoreSlim connectingLock = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim writingLock = new SemaphoreSlim(1, 1);

        private readonly TcpClient _client;
        private readonly Thread _worker;

        private NetworkStream _stream;
        private StreamReader _reader;


    }
}
