// -----------------------------------------------------------------------------
//  <copyright file="RenegadeCommunicator.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Net.GameServer
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Text;
    
    /// <summary>
    ///     <para>Provides access to a Renegade server's remote administration console.</para>
    /// </summary>
    public class RenegadeCommunicator : ServerCommunicator
    {
        private string _szHost;
        private int _iPort;
        private UdpClient _udpClient;
        private string _szPassword;
        private byte[] _abPassword;
        private TimeSpan _tsPasswordInterval;
        private DateTime _dtLastPassword = DateTime.MinValue;
        private byte[] _abPing; // password message. We send this every _tsPasswordInterval so cache it

        public RenegadeCommunicator(string serverAddress, string remoteAdminPass, int remoteAdminPort, TimeSpan? passwordInterval = null) : base(serverAddress, remoteAdminPort, remoteAdminPass)
        {
            if (serverAddress == null
                || remoteAdminPort > ushort.MaxValue
                || remoteAdminPort < ushort.MinValue
                || remoteAdminPass == null)
            {
                throw new ArgumentException("Host, port, or password were invalid");
            }

            _szHost = serverAddress;
            _iPort = remoteAdminPort;
            _szPassword = remoteAdminPass;
            _abPassword = Encoding.ASCII.GetBytes(remoteAdminPass);
            _tsPasswordInterval = passwordInterval ?? TimeSpan.FromSeconds(60);
            _abPing = GetPacket(_szPassword);

            if (_abPassword.Length != 8)
            {
                throw new ArgumentException("Valid passwords are exactly eight ASCII characters long");
            }

            _udpClient = new UdpClient(_szHost, _iPort);
        }

        public override void Write(string format, params object[] args)
        {
            Send(format, args);
        }

        #region Implementation of IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                    _udpClient = null;
                }
            }
        }

        #endregion

        /// <summary>
        /// Sends a formatted message to the Renegade server
        /// </summary>
        /// <param name="format">Format of the message to send. See String.Format for details</param>
        /// <param name="parm">Parameters for the format</param>
        public void Send(string format, params Object[] parm)
        {
            Send(string.Format(format, parm));
        }

        /// <summary>
        /// Sends a message to the renegade server
        /// </summary>
        /// <param name="message">Message to send to the renegade server</param>
        public void Send(string message)
        {
            // Line cannot be longer than 249 characters or FDS can crash
            if (message.Length > 249)
            {
                message = message.Substring(0, 249);
            }

            byte[] abMessage = GetPacket(message);
            if (DateTime.Now - _dtLastPassword > _tsPasswordInterval)
            {
                _dtLastPassword = DateTime.Now;
                _udpClient.Send(_abPing, _abPing.Length);
            }
            _udpClient.Send(abMessage, abMessage.Length);
        }

        // IDisposable::Dispose


        /// <summary>
        /// Private method to construct an encrypted packet to send
        /// </summary>
        /// <param name="message"></param>
        /// <returns>The packet</returns>
        private byte[] GetPacket(string message)
        {
            Debug.Assert(_abPassword.Length == 8); // Enforced by constructor

            byte[] abMessage    = Encoding.ASCII.GetBytes(message);
            byte[] abPassword   = new byte[_abPassword.Length];
            byte[] abPacket     = new byte[4 + 4 + abMessage.Length + 1]; // Holds final packet as checksum (4 bytes) + 4 null bytes + message + \0 terminator
            int iLength         = abMessage.Length + 5; // the 4 nulls and \0 terminator

            Array.Copy(_abPassword, abPassword, _abPassword.Length); // Copy password because we have to modify it for rounds of this message
            Array.Copy(abMessage, 0, abPacket, 8, abMessage.Length); // Copy message to byte 8 - (N-2) in the array
            
            // Encryption
            for (int i = 0; i < iLength; ++i)
            {
                abPacket[i + 4] = (byte)
                                  (
                                      (
                                          (
                                              (
                                                  (0xFF << 8) | (abPacket[i + 4] + i)
                                              ) - 0x32
                                          ) & 0xFF
                                      ) ^ abPassword[i % 8]
                                  );
                // Password copy is updated for subsequent rounds
                abPassword[i % 8] ^= abPacket[i + 4];
            }

            // Calculate checksum
            uint uiChecksum = 0;
            for (int i = 0; i < iLength; i += 4)
            {
                uiChecksum = unchecked(uiChecksum >> 31) + (uiChecksum * 2);

                // The next input to the checksum is the next 4-byte int with 0 byte padding
                byte b1 = i + 4 < abPacket.Length ? abPacket[i + 4] : (byte)0;
                byte b2 = i + 5 < abPacket.Length ? abPacket[i + 5] : (byte)0;
                byte b3 = i + 6 < abPacket.Length ? abPacket[i + 6] : (byte)0;
                byte b4 = i + 7 < abPacket.Length ? abPacket[i + 7] : (byte)0;
                unchecked
                {
                    uint uiVal = (uint)(b4 << 24 | b3 << 16 | b2 << 8 | b1);
                    uiChecksum += uiVal;
                }
            }

            // Place checksum into packet
            abPacket[0] = (byte)(uiChecksum & 0x000000FF);
            abPacket[1] = (byte)((uiChecksum & 0x0000FF00) >> 8);
            abPacket[2] = (byte)((uiChecksum & 0x00FF0000) >> 16);
            abPacket[3] = (byte)((uiChecksum & 0xFF000000) >> 24);

            return abPacket;
        }
    }
}
