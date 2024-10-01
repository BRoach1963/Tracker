using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;

namespace Tracker.Managers
{
    public class TrackerDataManager
    {
        #region Fields

        protected volatile bool _initialized;

        private List<TeamMember>? _teamMembers = new();

        #endregion

        #region Singleton Instance

        private static TrackerDataManager? _instance;
        private static readonly object SyncRoot = new object();

        public static TrackerDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new TrackerDataManager();
                    }
                }

                return _instance;
            }
        }

        public void Initialize()
        {
            if (_initialized) return; 

            _initialized = true;
        }

        public void Shutdown()
        {
            _teamMembers?.Clear();

            _teamMembers = null;
        }

        #endregion

        #region Public Properties

        public List<TeamMember>? TeamMembers => _teamMembers;

        #endregion

        #region Public Methods

        public async Task<List<TeamMember>> GetTeamData()
        {
            _teamMembers?.Clear();
            _teamMembers = await TrackerDbManager.Instance.GetTeamMembers().ConfigureAwait(true);
            return _teamMembers;
        }

        public async void UpdateTeamMember(int id, Dictionary<string, object> values)
        {
            await TrackerDbManager.Instance.UpdateTeamMemberValues(id, values);
        }

        public async void AddTeamMember(string? firstName = null,
            string? lastName = null,
            string? nickName = null,
            string? email = null,
            string? cell = null,
            string? jobTitle = null,
            DateTime? birthday = null,
            DateTime? hireDate = null,
            DateTime? terminationDate = null,
            bool? isActive = null,
            int? managerId = null,
            byte[]? profileImage = null,
            string? linkedInProfile = null,
            string? facebookProfile = null,
            string? instagramProfile = null,
            string? xProfile = null,
            int? specialty = null,
            int? skill = null,
            int? role = null)
        {
            var success = await TrackerDbManager.Instance.AddTeamMember(firstName, lastName, nickName, email, cell, jobTitle,
                birthday, hireDate, terminationDate, isActive, managerId, profileImage, linkedInProfile,
                facebookProfile, instagramProfile, xProfile, specialty, skill, role);

            //Update the full list of team members or just add this new one?  Not sure what I want to do here. 

            Messenger.Publish(new PropertyChangedMessage()
            {
                ChangedProperty = PropertyChangedEnum.TeamMembers,
                RefreshData = true
            });

        }

        public async void DeleteTeamMember(int id)
        {
            var success = await TrackerDbManager.Instance.DeleteTeamMember(id);
            Messenger.Publish(new PropertyChangedMessage()
            {
                ChangedProperty = PropertyChangedEnum.TeamMembers,
                RefreshData = true
            });
        }

        #endregion

        #region Private Methods


        #endregion


    }
}
