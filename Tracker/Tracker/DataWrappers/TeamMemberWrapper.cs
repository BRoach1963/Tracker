using Tracker.DataModels;

namespace Tracker.DataWrappers
{
    public class TeamMemberWrapper : BaseDataWrapper
    {
        #region Fields

        private TeamMember? _data;
        #endregion

        private TeamMemberWrapper(TeamMember? data = null)
        {
            _data = data ?? new TeamMember();
        }
    }
}
