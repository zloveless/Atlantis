namespace Atlantis.Net.Irc;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using JetBrains.Annotations;

/// <summary>
///     Provides a connection manager for an IRC connection.
/// </summary>
[PublicAPI]
public class IrcConnection : IDisposable
{
    public delegate void AuthenticateSsl(SslStream stream);

    public delegate void OnConnect();

    public delegate void OnDataReceived(string data);

    private static readonly Encoding Encoding = Encoding.UTF8;
    private readonly AuthenticateSsl? _authenticateSslHandler;
    private readonly OnConnect _connectHandler;
    private readonly SemaphoreSlim _connectingLock = new(0, 1);

    private readonly OnDataReceived _dataReceived;
    private readonly SemaphoreSlim _writingLock = new(1, 1);

    private TcpClient _client;
    private bool _stopRequested;
    private Stream _stream;

    public IrcConnection(OnConnect connectHandler, OnDataReceived dataReceivedHandler,
        AuthenticateSsl? authenticateSslHandler = null)
    {
        _connectHandler = connectHandler;
        _dataReceived = dataReceivedHandler;
        _authenticateSslHandler = authenticateSslHandler;
    }

    /// <summary>
    ///     Gets a value representing whether the connection is active.
    /// </summary>
    [PublicAPI]
    public bool Connected => _client != null && _client.Connected;

    /// <summary>
    ///     Gets the IRC address in which to connect.
    /// </summary>
    public string HostName { get; set; } = "";

    /// <summary>
    ///     Gets the port on which to connect.
    /// </summary>
    public short Port { get; set; } = 6667;

    /// <summary>
    ///     Gets or sets a value indicating whether the connection will use SSL.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        _client.Dispose();
        _stream.Dispose();
        _connectingLock.Dispose();
        _writingLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnDisconnect()
    {
        SocketDisconnectEvent?.Invoke(this, EventArgs.Empty);
    }

    private async Task ReceiveCallback()
    {
        var lastMessageIndex = 0;
        using var reader = new StreamReader(_stream, Encoding, leaveOpen: true);
        while (Connected)
        {
            if (lastMessageIndex > 3)
            {
                break;
            }

            var data = await reader.ReadLineAsync();
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
    ///     Sends the specified formatted message to the IRC connection synchronously, if connected.
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
    ///     Sends the specified formatted message to the IRC connection asynchronously, if connected.
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

    /// <summary>
    ///     Starts the <see cref="IrcConnection" />.
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public async Task<bool> Start(CancellationToken cancellationToken)
    {
        try
        {
            _client?.Close();
            _client = new TcpClient();

            var he = await Dns.GetHostEntryAsync(HostName, cancellationToken).ConfigureAwait(false);
            var connection = new IPEndPoint(he.AddressList[0], Port);
            await _client.ConnectAsync(connection, cancellationToken);
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

            _ = Task.Run(ReceiveCallback, cancellationToken);
            _connectHandler();
        }
        catch (SocketException)
        {
            return false;
        }

        await _connectingLock.WaitAsync(cancellationToken);
        return true;
    }

    public void Stop()
    {
        _stopRequested = true;
        _client.Close();
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain,
        SslPolicyErrors sslpolicyerrors)
    {
        // TODO: validate?
        return true;
    }

    /// <summary>
    ///     Raised when the connection ends prematurely.
    /// </summary>
    public event EventHandler SocketDisconnectEvent;
}