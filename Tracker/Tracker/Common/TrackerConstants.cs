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

        public const string TeamMemberId = "@Id";
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

        #endregion

        #region Sql Stored Procedures

        public const string UpdateTeamMember = "UpdateTeamMember";
        public const string AddTeamMember = "AddTeamMember";
        public const string DeleteTeamMember = "DeleteTeamMember";
        public const string GetTeamMembers = "GetTeamMembers";
        public const string GetTeamMember = "GetTeamMember";

        #endregion

        #region Sql Field Names

        public const string IdField = "Id";

        #endregion
    }
}
