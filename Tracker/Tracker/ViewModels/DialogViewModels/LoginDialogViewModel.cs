using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;

namespace Tracker.ViewModels.DialogViewModels
{
    public class LoginDialogViewModel : BaseDialogViewModel
    {
        #region Fields

        private ICommand _loginCommand;
        private ICommand _cancelCommand;

        private string _userName;
        private string _password;

        private bool _useWindows = true;

        #endregion

        #region Ctor

        public LoginDialogViewModel(Action? callback) : base(callback)
        {
            
        }


        #endregion

        #region Public Properties

        public DialogResult Result { get; set; }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                RaisePropertyChanged(nameof(UserName));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RaisePropertyChanged(nameof(UserName));
            }
        }

        public bool UseWindowsAuthentication
        {
            get => _useWindows;
            set
            {
                _useWindows = value;
                RaisePropertyChanged(nameof(UseWindowsAuthentication));
            }
        }
        #endregion

        #region Commands

        public ICommand LoginCommand =>
            _loginCommand ??= new TrackerCommand(LoginCommandExecuted, CanExecuteLoginCommand);

        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(CancelCommandExecuted);

        #endregion

        #region Private Methods

        private void CancelCommandExecuted(object? obj)
        {
            DialogResult = new DialogResult()
            {
                Cancelled = true
            };
            Callback?.Invoke();
        }

        private bool CanExecuteLoginCommand(object? obj)
        {
            if (UseWindowsAuthentication)
            {
                return true;
            }

            return !string.IsNullOrEmpty(_password) && !string.IsNullOrEmpty(_userName);
        }

        private void LoginCommandExecuted(object? obj)
        {
             //NOTE: Do something else here to login to db.  Have to do the check here maybe?  Not sure.

             DialogResult = new DialogResult()
             {
                 Cancelled = false
             };
             Callback?.Invoke();
        }


        #endregion
    }
}
