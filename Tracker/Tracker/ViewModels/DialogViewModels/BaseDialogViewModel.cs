using Tracker.Classes;

namespace Tracker.ViewModels.DialogViewModels
{
    public class BaseDialogViewModel(Action? callback) : BaseViewModel
    {
        private DialogResult _result = new(); 

        public DialogResult DialogResult
        {
            get => _result;
            set
            {
                _result = value;
                RaisePropertyChanged();
            }
        }

        public virtual void SubscribeToMessages() { }

        public virtual void UnSubscribeFromMessages() { }

        public Action? Callback { get; set; } = callback;
 
    }
}
