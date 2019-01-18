// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Atlantis.Net.Irc
{
    public partial class IrcConnection
    {
        private void ThreadCallback(object arg0)
        {
            if (_config.Value.EnableSsl)
            {
                var baseStream = _socket.GetStream();
                var sslStream = new SslStream(baseStream, leaveInnerStreamOpen: false, userCertificateValidationCallback: ValidationCallback);
                var cert = GetCertificate(_config.Value.SslCertificate, _config.Value.SslCertificateKey);

                sslStream.AuthenticateAsClient(_host, new X509CertificateCollection(new[] { cert }), System.Security.Authentication.SslProtocols.Tls12, false);
                _networkStream = sslStream;
            }
            else
            {
                _networkStream = _socket.GetStream();
            }

            using (var reader = new StreamReader(_networkStream))
            {
                while (IsConnected)
                {
                    if (_cts.IsCancellationRequested) break;

                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;

                    OnDataReceived(line);
                }
            }
        }

        private bool ValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            // TODO: Do certificate subject and domain check?
            return true;
        }
        
        private X509Certificate GetCertificate(string clientCertificate, string certificatePrivateKeyFile)
        {
            return null;
        }

        protected virtual void OnDataReceived(string message)
        {
            if (_registrationCallback != null)
            {
                _registrationCallback(this);
                _registrationCallback = null;
            }

            var tokens = message.Split(' ');
            var tokenIndex = 0;

            IrcSource? source = null;
            if (tokens[tokenIndex][0] == ':')
            {
                source = IrcSource.Parse(tokens[tokenIndex]);
                tokenIndex++;
            }


        }
    }
}
