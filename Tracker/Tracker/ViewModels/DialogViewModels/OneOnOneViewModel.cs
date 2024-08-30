using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;
using Tracker.Helpers;

namespace Tracker.ViewModels.DialogViewModels
{
    public class OneOnOneViewModel : BaseDialogViewModel
    {
        public OneOnOneViewModel(Action? callback, OneOnOne data, bool edit = true) : base(callback)
        {
            
        }

    }
}
