using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using JetBrains.Annotations;

namespace Atlantis.Net.Irc;

[PublicAPI] public delegate void OnDataReceived(string data);
[PublicAPI] public delegate void OnConnect();

[PublicAPI] public delegate void AuthenticateSsl(SslStream stream);

/// <summary>
/// Provides a connection manager for an IRC connection.
/// </summary>
[PublicAPI]
public class IrcConnection
{
    private readonly OnDataReceived _dataReceived;
    private readonly AuthenticateSsl? _authenticateSslHandler;
    private readonly OnConnect _connectHandler;
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private readonly TcpClient _client = new();
    private readonly SemaphoreSlim _connectingLock = new(0, 1);
    private Stream _stream;
    private readonly SemaphoreSlim _writingLock = new(1, 1);
    private readonly Thread _worker;
    private bool _stopRequested;

    public IrcConnection(OnConnect connectHandler, OnDataReceived dataReceivedHandler, AuthenticateSsl? authenticateSslHandler = null)
    {
        _connectHandler = connectHandler;
        _dataReceived = dataReceivedHandler;
        _authenticateSslHandler = authenticateSslHandler;

        _worker = new Thread(ThreadCallback)
        {
            IsBackground = true
        };
    }

    /// <summary>
    /// Gets a value representing whether the connection is active.
    /// </summary>
    [PublicAPI]
    public bool Connected => _client != null && _client.Connected;
    
    /// <summary>
    /// Gets the IRC address in which to connect.
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// Gets the port on which to connect.
    /// </summary>
    public short Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the connection will use SSL.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Raised when the connection ends prematurely.
    /// </summary>
    public event EventHandler SocketDisconnectEvent;
    
    /// <summary>
    /// Starts the <see cref="IrcConnection" />.
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public async Task<bool> Start()
    {
        if (Connected)
        {
            return false;
        }
        
        try
        {
            var he = await Dns.GetHostEntryAsync(HostName).ConfigureAwait(false);
            var connection = new IPEndPoint(he.AddressList[0], Port);
            await _client.ConnectAsync(connection);
            var stream = _client.GetStream();
            
            if (UseSsl && _authenticateSslHandler != null)
            {
                var sslStream = new SslStream(stream, false, ValidateServerCertificate, null);
                _authenticateSslHandler(sslStream);
                _stream = sslStream;
            }
            else
            {
                _stream = stream;
            }
            
            _worker.Start();
            _connectHandler();
        }
        catch (SocketException)
        {
            return false;
        }

        await _connectingLock.WaitAsync();
        return true;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslpolicyerrors)
    {
        // TODO: validate?
        return true;
    }

    public Task<bool> Stop() 
    {
        if (!Connected)
        {
            return Task.FromResult(false);
        }

        _stopRequested = true;
        _client.Close();

        return Task.FromResult(true);
    }
    
    private void ThreadCallback(object? state)
    {
        var lastMessageIndex = 0;
        using var reader = new StreamReader(_stream, Encoding, leaveOpen: true);
        while (Connected)
        {
            if (lastMessageIndex > 3)
            {
                break;
            }
            
            var data = reader.ReadLine();
            if (!string.IsNullOrEmpty(data))
            {
                _dataReceived(data);
                lastMessageIndex = 0;
            }

            lastMessageIndex++;
        }
        
        if (!_stopRequested)
        {
            OnDisconnect();
        }
    }
    
    /// <summary>
    /// Sends the specified formatted message to the IRC connection synchronously, if connected.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool Send(string message, params object[] args) 
    {
        if (!Connected)
        {
            return false;
        }

        _writingLock.Wait();
        try
        {
            var compiled = new StringBuilder()
                           .AppendFormat(message, args)
                           .Append('\n')
                           .ToString();
            
            var buf = Encoding.GetBytes(compiled);
            _stream.Write(buf);
            _stream.Flush();
            return true;
        }
        finally
        {
            _writingLock.Release();
        }
    }
    
    /// <summary>
    /// Sends the specified formatted message to the IRC connection asynchronously, if connected.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [PublicAPI]
    public async Task<bool> SendAsync(string message, params object[] args)
    {
        if (!Connected)
        {
            return false;
        }
        
        await _writingLock.WaitAsync();
        try
        {
            var compiled = new StringBuilder()
                           .AppendFormat(message, args)
                           .Append('\n')
                           .ToString();

            var buf = Encoding.GetBytes(compiled);
            await _stream.WriteAsync(buf);
            await _stream.FlushAsync();
            return true;
        }
        finally
        {
            _writingLock.Release();
        }
    }
    
    protected virtual void OnDisconnect()
    {
        SocketDisconnectEvent?.Invoke(this, EventArgs.Empty);
    }
}
