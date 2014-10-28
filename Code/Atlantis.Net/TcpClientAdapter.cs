// -----------------------------------------------------------------------------
//  <copyright file="TcpClientAdapter.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net
{
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;

    [Obsolete]
	public class TcpClientAdapter : ITcpClient
	{
		private readonly TcpClient client;
		private readonly Encoding encoding;

		private StreamReader clientReader;
		private NetworkStream stream;

		public TcpClientAdapter(TcpClient client)
		{
			this.client = client;
			InitializeStreams();
		}

		public TcpClientAdapter(TcpClient client, Encoding encoding) : this(client)
		{
			this.encoding = encoding;
			InitializeStreams(encoding);
		}

		private void InitializeStreams(Encoding encoding = null)
		{
			if (client == null) return;

			stream = client.GetStream();

			clientReader = new StreamReader(stream, encoding ?? Encoding.Default);
			// clientWriter = new StreamWriter(stream, encoding ?? Encoding.Default);
		}

		private string BuildPacket(string format, params object[] args)
		{
			return new StringBuilder().AppendFormat(format, args).ToString();
		}

		private string BuildPackageNewLine(string format, params object[] args)
		{
			return new StringBuilder().AppendFormat(format, args).AppendLine().ToString();
		}

		#region Implementation of ITcpClient

		public bool Connected
		{
			get { return client.Connected; }
		}

		public bool DataAvailable
		{
			get { return stream.DataAvailable; }
		}

		public bool EndOfStream
		{
			get { return clientReader.EndOfStream; }
		}

	    public Stream BaseStream
	    {
	        get { return client.GetStream(); }
	    }

	    public void Connect(string host, int port)
		{
			var entry = Dns.GetHostEntry(host);
			if (entry == null)
			{
				throw new ArgumentNullException("host", "Unable to resolve host. Check network configuration.");
			}

			var connection = new IPEndPoint(entry.AddressList[0], port);
			client.Connect(connection);
		}

		public void Close()
		{
			client.Close();
		}

		public string ReadLine()
		{
			return clientReader.ReadLine();
		}

		public string ReadAll()
		{
			return clientReader.ReadToEnd();
		}

		public void Write(string format, params object[] args)
		{
			var message = BuildPacket(format, args);
			var buf = encoding.GetBytes(message);

			stream.Write(buf, 0, buf.Length);
			//			stream.Flush();
		}

		public void WriteLine(string format, params object[] args)
		{
			var message = BuildPackageNewLine(format, args);
			var buf = encoding.GetBytes(message);

			stream.Write(buf, 0, buf.Length);
			//			stream.Flush();
		}

		#endregion
	}
}
