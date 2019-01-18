// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using System;

namespace Atlantis.Net.Irc
{
    public partial class IrcConnection
    {
        public event EventHandler ConnectionEstablishedEvent;
    }
}
