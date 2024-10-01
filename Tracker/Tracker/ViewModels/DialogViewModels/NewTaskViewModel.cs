using Tracker.Interfaces;

namespace Tracker.ViewModels.DialogViewModels
{
    public class NewTaskViewModel : BaseDialogViewModel
    {
        public NewTaskViewModel(Action? callback, ITask data, bool edit = true) : base(callback)
        {

        }
    }
}
