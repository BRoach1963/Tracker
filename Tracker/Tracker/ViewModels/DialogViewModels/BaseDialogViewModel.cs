using Tracker.Classes;

namespace Tracker.ViewModels.DialogViewModels
{
    public class BaseDialogViewModel : BaseViewModel
    {
        private DialogResult _result = new();

        public BaseDialogViewModel(Action? callback)
        {
            Callback = callback;
        }

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

        public Action? Callback { get; set; }
 
    }
}
