// -----------------------------------------------------------------------------
//  <copyright file="ITcpClientAsync.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net
{
	using System.Threading.Tasks;

	public interface ITcpClientAsync : ITcpClient
	{
		Task ConnectAsync(string host, int port);

		Task<string> ReadLineAsync();

		Task<string> ReadAllAsync();

		Task WriteAsync(string format, params object[] args);

		Task WriteLineAsync(string format, params object[] args);
	}
}
