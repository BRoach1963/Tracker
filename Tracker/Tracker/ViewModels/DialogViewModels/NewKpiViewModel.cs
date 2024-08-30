using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.ViewModels.DialogViewModels
{
    public class NewKpiViewModel : BaseDialogViewModel
    {
        public NewKpiViewModel(Action? callback, KeyPerformanceIndicator data, bool edit = true) : base(callback)
        {

        }
    }
}
