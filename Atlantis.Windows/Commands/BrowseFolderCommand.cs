// -----------------------------------------------------------------------------
//  <copyright file="BrowseFolderCommand.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Windows.Commands
{
    using System;
    using WF = System.Windows.Forms;
    using System.Windows.Input;
    
    public class BrowseFolderCommand : MvvmCommandBase
    {
        private readonly Func<object, bool> _condition;

        public BrowseFolderCommand(Func<object, bool> conditional) : base(conditional)
        {
        }

        /// <summary>
        ///     <para>Raised when the browse dialog returns a value.</para>
        /// </summary>
        public event EventHandler<FolderFileBrowseEventArgs> FolderBrowseEvent;
        
        /// <inheritdoc />
        public override void Execute(object parameter)
        {
            using (var browser = new WF.FolderBrowserDialog())
            {
                browser.SelectedPath = parameter as string ?? "";
                browser.ShowNewFolderButton = false;

                var result = browser.ShowDialog();
                if (result == WF.DialogResult.OK)
                {
                    FolderBrowseEvent?.Invoke(this, new FolderFileBrowseEventArgs(browser.SelectedPath));
                }
            }
        }
    }
}
