// -----------------------------------------------------------------------------
//  <copyright file="IrcClient.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.Irc
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    // RFC 1459: https://tools.ietf.org/html/rfc1459

    public partial class IrcClient
    {
	    #region Fields

	    private readonly Queue<String> messageQueue = new Queue<String>();
	    private readonly IDictionary<String, Channel> channels = new ConcurrentDictionary<String, Channel>();

	    private readonly SemaphoreSlim connectingLock = new SemaphoreSlim(0, 1);
	    private readonly SemaphoreSlim writingLock = new SemaphoreSlim(1, 1);

	    private readonly TcpClient client;
	    private readonly Thread worker;
	    private readonly Thread qWorker;

	    private NetworkStream stream;
	    private StreamReader reader;
	    private bool requestShutdown;
	    private string currentNick;

	    #endregion

	    #region Constructors

	    public IrcClient()
	    {
		    client = new TcpClient();
		    worker = new Thread(WorkerCallback);
		    qWorker = new Thread(QueueWorkerCallback);
			Modes = new ModeCollection();

		    Encoding = Encoding.UTF8;

		    //ConnectionTimeOutEvent += OnTimeout;

		    QueueInterval = 1000;
	    }

	    public IrcClient(IrcConfiguration config) : this()
	    {
		    Encoding = config.Encoding;
		    HostName = config.Host;
		    Ident = config.Ident;
		    Nick = config.Nick;
		    Password = config.Password;
		    Port = config.Port;
		    RealName = config.RealName;

		    if (config.SslEnabled)
		    {
			    Options |= ConnectOptions.Secure;
		    }
	    }

	    #endregion
		
        #region Events

        public event EventHandler ConnectionEstablishedEvent;
        public event EventHandler<TimeoutEventArgs> ConnectionTimeOutEvent;
		public event EventHandler<MessageReceivedEventArgs> NoticeReceivedEvent;
		public event EventHandler<MessageReceivedEventArgs> PrivmsgReceivedEvent;
		public event EventHandler<RfcNumericReceivedEventArgs> RfcNumericReceivedEvent;
		public event EventHandler<JoinPartEventArgs> JoinEvent;
	    public event EventHandler<ModeChangedEventArgs> ModeChangedEvent;
	    public event EventHandler<JoinPartEventArgs> PartEvent;
	    public event EventHandler<QuitEventArgs> QuitEvent;

		#endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the socket is connected to the IRC server.
        /// </summary>
        public bool Connected
        {
            get { return client != null && client.Connected; }
        }

		/// <summary>
		///		<para>Gets or sets a value indicating whether to enable ircv3 features with the IrcClient.</para>
		///		<para>Defaults to false.</para>
		/// </summary>
		public bool EnableV3 { get; set; }

        public Encoding Encoding { get; set; }
		
		public bool FillListsOnJoin { get; set; }

        /// <summary>
        /// Gets or sets the host indicating the location of the IRC server.
        /// </summary>
        public String HostName { get; set; }

        /// <summary>
        /// Gets or sets a value representing our user info on IRC.
        /// </summary>
        public String Ident { get; set; }

        /// <summary>
        /// Gets a value indicating whether the IrcClient has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                bool ret = true;

                if (String.IsNullOrEmpty(HostName)) ret = false;
                else if (String.IsNullOrEmpty(Nick)) ret = false;

                return ret;
            }
        }

		/// <summary>
		/// Gets a collection of modes set on the IrcClient.
		/// </summary>
		public ModeCollection Modes { get; private set; }

        /// <summary>
        /// Gets or sets the nick that represents us on the IRC server.
        /// </summary>
        public String Nick { get; set; }

        /// <summary>
        /// Gets or sets options for connecting to the IRC server.
        /// </summary>
        public ConnectOptions Options { get; set; }

        /// <summary>
        /// Gets or sets a value representing the password used for connecting to the IRC server.
        /// </summary>
        public String Password { get; set; }

        /// <summary>
        /// Gets or sets the port for connecting to the IRC server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the interval, in milliseconds, the queue worker processes enqueued messages.
        /// </summary>
        public int QueueInterval { get; set; }

        /// <summary>
        /// Gets or sets the realname of ourself on the IRC server.
        /// </summary>
        public String RealName { get; set; }
        
        #endregion

        #region Methods

        /// <summary>
        /// Returns a channel from the internal collection of the IrcClient.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        /// <exception cref="T:Atlantis.Net.Irc.RfcException" />
        public Channel GetChannel(String channelName)
        {
            Channel c;

	        lock (channels)
	        {
		        if (channels.TryGetValue(channelName, out c))
		        {
			        return c;
		        }

		        if (info.ChannelLength > 0 && channelName.Length > info.ChannelLength)
		        { // Check if the channel length is greater than zero (whether we have it set) and whether the channel name specified conforms to that length.
			        throw new RfcException(String.Format("The length of the channel {0} cannot exceed a length of {1} as provided by the IRC server.", channelName, info.ChannelLength));
		        }

		        c = new Channel(this, channelName);
		        channels.Add(c.Name, c);
	        }

            return c;
        }

	    public void RemoveChannel(String channelName)
	    {
		    lock (channels)
		    {
			    if (channels.ContainsKey(channelName))
			    {
				    channels.Remove(channelName);
			    }
		    }
	    }

	    protected virtual void SetDefaultValues()
        {
            if (String.IsNullOrEmpty(Ident))
            {
                Ident = Nick.ToLower();
            }

            if (String.IsNullOrEmpty(RealName))
            {
                RealName = Nick;
            }

            if(Port == 0)
            {
                Port = 6667;
            }
        }

	    public async void SetNick(String newNick)
	    {
		    await SendNow("NICK {0}", newNick);
		    currentNick = newNick;
	    }

	    private async void Send(String message)
        {
            if (!Connected) return;
            
            await writingLock.WaitAsync();

            byte[] buf = Encoding.GetBytes(message);
            await stream.WriteAsync(buf, 0, buf.Length);
            await stream.FlushAsync();

            writingLock.Release();
        }

        /// <summary>
        /// Appends the specified message into the message queue for writing to the IRC server.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [StringFormatMethod("format")]
        public void Send(String format, params object[] args)
        {
            if (!Connected) return;

            var sb = new StringBuilder();
            sb.AppendFormat(format, args).Append('\n');

            lock (messageQueue)
            {
                messageQueue.Enqueue(sb.ToString());
            }
        }

        /// <summary>
        /// Sends the specified formatted message to the IRC server without waiting for the message queue.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<bool> SendNow(String format, params object[] args)
        {
            if (!Connected) return false;

            await writingLock.WaitAsync();

            var message = new StringBuilder();
            message.AppendFormat(format, args).Append('\n');

            byte[] buf = Encoding.GetBytes(message.ToString());
            await stream.WriteAsync(buf, 0, buf.Length);
            await stream.FlushAsync();

            writingLock.Release();

            return true;
        }

        public async Task<bool> Start()
        {
            if (!IsInitialized)
            {
                return false;
            }

            SetDefaultValues();
            try
            {
                var connection = new IPEndPoint(Dns.GetHostEntry(HostName).AddressList[0], Port);
                client.Connect(connection);
            }
            catch (SocketException e)
            {
                // TODO: Raise disconnection event.
                var exc = e;
            }

            worker.IsBackground = true;
            worker.Start();

            await connectingLock.WaitAsync();
            return true;
        }

        public async void Stop(String reason = null)
        {
            if (Connected)
            {
                requestShutdown = true;
	            if (String.IsNullOrEmpty(reason))
	            {
		            await SendNow("QUIT");
	            }
	            else
	            {
		            await SendNow("QUIT :{0}", reason);
	            }
            }
        }

        #endregion
    }

    #region External type: RfcException

	/// <summary>
	/// 
	/// </summary>
    public class RfcException : Exception
    {
        public RfcException(String message) : base(message)
        {
        }
    }

    #endregion
}
