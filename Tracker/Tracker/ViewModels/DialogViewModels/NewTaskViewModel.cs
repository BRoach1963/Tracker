using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;
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
