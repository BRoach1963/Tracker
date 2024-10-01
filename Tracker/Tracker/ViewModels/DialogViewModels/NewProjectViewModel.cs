using Tracker.DataModels;

namespace Tracker.ViewModels.DialogViewModels
{
    public class NewProjectViewModel : BaseDialogViewModel
    {
        public NewProjectViewModel(Action? callback, Project data, bool edit = true) : base(callback)
        {

        }
    }
}
