// -----------------------------------------------------------------------------
//  <copyright file="BrowseFileCommand.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Windows.Commands
{
    using WF = System.Windows.Forms;
    using System;
    using System.Windows.Input;

    internal class BrowseFileCommand : ICommand
    {
        private readonly Func<object, bool> _condition;

        public BrowseFileCommand(Func<object, bool> conditional)
        {
            _condition = conditional;
        }

        public bool IsOpenReadOnly { get; set; }

        #region Implementation of ICommand

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return _condition == null || _condition(parameter);
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            using (var browser = new WF.OpenFileDialog())
            {
                browser.ShowReadOnly = IsOpenReadOnly;
            }
        }
        
        /// <inheritdoc />
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        #endregion
    }
}
