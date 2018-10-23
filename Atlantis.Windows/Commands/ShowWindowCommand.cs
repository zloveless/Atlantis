// -----------------------------------------------------------------------------
//  <copyright file="ShowWindowCommand.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------
namespace Atlantis.Windows.Commands
{
    using System;
    using System.Windows;

    /// <inheritdoc />
    public class ShowWindowCommand<T> : MvvmCommandBase where T : Window, new()
    {
        private readonly object _parameters;

        /// <inheritdoc />
        public ShowWindowCommand(Func<object, bool> conditional, object parameters = null) : base(conditional)
        {
            _parameters = parameters;
        }

        /// <summary>
        ///     <para>Gets or sets a handle to a Window representing the owner of the spawned window.</para>
        /// </summary>
        public Window Owner { get; set; }

        /// <summary>
        ///     <para>Gets or sets a value indicating whether to capture the return value as a dialog.</para>
        /// </summary>
        public bool UseDialogReturn { get; set; }

        public event EventHandler<WindowResultEventArgs> WindowResultEvent;

        #region Overrides of MvvmCommandBase

        /// <inheritdoc />
        public override void Execute(object parameter)
        {
            var w = (Window)Activator.CreateInstance(typeof(T));

            w.Owner       = Owner;
            w.DataContext = parameter;
            
            

            w.Show();

            if (UseDialogReturn)
            {
                var result = w.DialogResult;
                if (result != null)
                {
                    WindowResultEvent?.Invoke(this, new WindowResultEventArgs(result.Value));
                }
            }
        }

        #endregion
    }
}
