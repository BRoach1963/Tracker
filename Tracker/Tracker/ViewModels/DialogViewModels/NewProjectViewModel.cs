using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
