// -----------------------------------------------------------------------------
//  <copyright file="TcpClientAsyncAdapter.cs" company="Zack Loveless">
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
	using System.Threading.Tasks;

    [Obsolete]
	public class TcpClientAsyncAdapter : ITcpClientAsync
	{
		private readonly TcpClient client;
		private Encoding encoding;
		private StreamReader reader;
		private NetworkStream stream;

		public TcpClientAsyncAdapter(TcpClient client, Encoding encoding)
		{
			this.client   = client;
			this.encoding = encoding;
		}

		private void InitializeAdapter(Task task)
		{
			if (client == null) return;

			stream = client.GetStream();
			encoding = encoding ?? new UTF8Encoding(false);
			reader = new StreamReader(client.GetStream(), encoding);
		}

		#region Implementation of ITcpClient

		public bool Connected
		{
			get { return client != null && client.Connected; }
		}

		public bool DataAvailable
		{
			get { return stream != null && stream.DataAvailable; }
		}

		public bool EndOfStream
		{
			get { return reader.EndOfStream; }
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

			InitializeAdapter(null);
		}

		public void Close()
		{
			client.Close();
		}

		public string ReadLine()
		{
			var result = reader.ReadLineAsync();

			return result.Result;
		}

		public string ReadAll()
		{
			var result = reader.ReadToEndAsync();

			return result.Result;
		}

		public void Write(string format, params object[] args)
		{
			var s   = string.Format(format, args);
			var buf = encoding.GetBytes(s);

			stream.Write(buf, 0, buf.Length);
			stream.Flush();
		}

		public void WriteLine(string format, params object[] args)
		{
			var s = new StringBuilder();
			if (args.Length == 0) s.Append(format);
			else s.AppendFormat(format, args);
			s.AppendLine();

			var buf = encoding.GetBytes(s.ToString());

			stream.Write(buf, 0, buf.Length);
			stream.Flush();
		}

		#endregion

		#region Implementation of ITcpClientAsync
		
		public Task ConnectAsync(string host, int port)
		{
			return client.ConnectAsync(host, port).ContinueWith(InitializeAdapter);
		}

		public Task<string> ReadLineAsync()
		{
			return reader.ReadLineAsync();
		}

		public Task<string> ReadAllAsync()
		{
			return reader.ReadToEndAsync();
		}

		public Task WriteAsync(string format, params object[] args)
		{
			var s = string.Format(format, args);

			var buf = encoding.GetBytes(s);
			return stream.WriteAsync(buf, 0, buf.Length).ContinueWith(x => stream.Flush());
		}

		public Task WriteLineAsync(string format, params object[] args)
		{
			var s = new StringBuilder();
			s.AppendFormat(format, args);
			s.AppendLine();

			var buf = encoding.GetBytes(s.ToString());
			return stream.WriteAsync(buf, 0, buf.Length).ContinueWith(x => stream.Flush());
		}

		#endregion
	}
}
