using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.ViewModels.DialogViewModels
{
    public class NewOkrViewModel : BaseDialogViewModel
    {
        public NewOkrViewModel(Action? callback, ObjectiveKeyResult data, bool edit = true) : base(callback)
        {

        }
    }
}
