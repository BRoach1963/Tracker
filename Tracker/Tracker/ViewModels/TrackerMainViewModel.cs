using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;
using Tracker.MockData;

namespace Tracker.ViewModels
{
    public class TrackerMainViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<TeamMember> _teamMembers = MockTeamMemberData.GetMockTeamMemberData();

        #endregion


        #region Public Properties

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        #endregion
    }
}
