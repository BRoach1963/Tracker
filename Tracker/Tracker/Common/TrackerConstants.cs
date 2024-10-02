using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tracker.Common
{
    public static class TrackerConstants
    {
        #region SQL Parameters

        public const string GenericId = "@Id";
        public const string TeamMemberFirstName = "@FirstName";
        public const string TeamMemberLastName = "@LastName";
        public const string TeamMemberNickname = "@NickName";
        public const string TeamMemberEmail = "@Email";
        public const string TeamMemberCell = "@Cell";
        public const string TeamMemberJobTitle = "@JobTitle";
        public const string TeamMemberHireDate = "@HireDate";
        public const string TeamMemberTerminationDate = "@TerminationDate";
        public const string TeamMemberIsActive = "@IsActive";
        public const string TeamMemberManagerId = "@ManagerId";
        public const string TeamMemberProfileImage = "@ProfileImage";
        public const string TeamMemberLinkedInProfile = "@LinkedInProfile";
        public const string TeamMemberFacebookProfile = "@FacebookProfile";
        public const string TeamMemberInstaProfile = "@InstaProfile";
        public const string TeamMemberXProfile = "@XProfile";
        public const string TeamMemberSpeciality = "@Speciality";
        public const string TeamMemberSkill = "@Skill";
        public const string TeamMemberRole = "@Role";
        public const string TeamMemberBirthday = "@Birthday";

        public const string OneOnOneDescription = "@Description";
        public const string OneOnOneDate = "@Date";
        public const string OneOnOneStartTime = "@StartTime";
        public const string OneOnOneDuration = "@Duration";
        public const string OneOnOneAgenda = "@Agenda";
        public const string OneOnOneNotes = "@Notes";
        public const string OneOnOneIsRecurring = "@IsRecurring";
        public const string OneOnOneFeedback = "@Feedback"; 
        public const string OneOnOneStatus = "@Status";
        public const string OneOnOneTeamMemberId = "@TeamMemberId"; 

        #endregion

        #region Sql Stored Procedures

        public const string UpdateTeamMember = "UpdateTeamMember";
        public const string AddTeamMember = "AddTeamMember";
        public const string DeleteTeamMember = "DeleteTeamMember";
        public const string GetTeamMembers = "GetTeamMembers";
        public const string GetTeamMember = "GetTeamMember";

        public const string UpdateOneOnOne = "UpdateOneOnOne";
        public const string AddNewOneOnOne = "AddNewOneOnOne";
        public const string GetOneOnOnes = "GetOneOnOnes";
        public const string DeleteOneOnOne = "DeleteOneOnOne";

        public const string AddNewTask = "AddNewTask";
        public const string UpdateTask = "UpdateTask";
        public const string GetTasks = "GetTasks";
        public const string DeleteTask = "DeleteTask";

        public const string AddNewProject = "AddNewProject";
        public const string UpdateProject = "UpdateProject";
        public const string GetProjects = "GetProjects";
        public const string DeleteProject = "DeleteProject";

        public const string AddNewKpi = "AddNewKpi";
        public const string UpdateKpi = "UpdateKpi";
        public const string GetKpis = "GetKpis";
        public const string DeleteKpi = "DeleteKpi";

        public const string AddNewOkr = "AddNewOkr";
        public const string UpdateOkr = "UpdateOkr";
        public const string GetOkrs = "GetOkrs";
        public const string DeleteOkr = "DeleteOkr";

        public const string AddNewDiscussionPoint = "AddNewDiscussionPoint";
        public const string UpdateDiscussionPoint = "UpdateDiscussionPoint";
        public const string GetDiscussionPoints = "GetDiscussionPoints";
        public const string DeleteDiscussionPoint = "DeleteDiscussionPoint";

        public const string AddNewConcern = "AddNewConcern";
        public const string UpdateConcern = "UpdateConcern";
        public const string GetConcerns = "GetConcerns";
        public const string DeleteConcern = "DeleteConcern";

        #endregion

        #region Sql Field Names

        public const string IdField = "Id";

        #endregion
    }
}
