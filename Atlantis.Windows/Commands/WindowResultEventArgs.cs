// -----------------------------------------------------------------------------
//  <copyright file="WindowResultEventArgs.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Windows.Commands
{
    public class WindowResultEventArgs : System.EventArgs
    {
        public WindowResultEventArgs(bool result)
        {
            Result = result;
        }

        /// <summary>
        ///     <para>Gets a value representing the result of the window having been shown.</para>
        /// </summary>
        public bool Result { get; }
    }
}
