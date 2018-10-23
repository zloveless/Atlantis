// -----------------------------------------------------------------------------
//  <copyright file="FolderFileBrowseEventHandler.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Windows.Commands
{
    using System;

    public class FolderFileBrowseEventArgs : EventArgs
    {
        public FolderFileBrowseEventArgs(string path)
        {
            Path = path;
        }

        /// <summary>
        ///     <para>Gets the path result from the associated dialog pop-up.</para>
        /// </summary>
        public string Path { get; }
    }
}
