// -----------------------------------------------------------------------------
//  <copyright file="MvvmCommandBase.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Windows.Commands
{
    using System;
    using System.Windows.Input;
    
    /// <inheritdoc />
    /// <summary>
    ///     <para>Represents a base command class that implements <see cref="T:System.Windows.Input.ICommand" />.</para>
    /// </summary>
    public abstract class MvvmCommandBase : ICommand
    {
        private readonly Func<object, bool> _condition;

        protected MvvmCommandBase(Func<object, bool> conditional)
        {
            _condition = conditional;
        }

        #region Implementation of ICommand

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return _condition == null || _condition(parameter);
        }

        /// <inheritdoc />
        public abstract void Execute(object parameter);

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        #endregion
    }
}
