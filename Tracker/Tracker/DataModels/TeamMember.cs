using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class TeamMember
    {
        #region Ctor
 

        #endregion

        #region Public Properties

        public int Id { get; set; } = 0;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string NickName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CellPhone { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public DateTime BirthDay { get; set; } = DateTime.MinValue;

        public DateTime HireDate { get; set; } = new DateTime(1900, 1, 1);

        public DateTime TerminationDate { get; set; } = new DateTime(1900, 1, 1);

        public bool IsActive { get; set; } = true;

        public int ManagerId { get; set; } = 0;

        public byte[] ProfileImage { get; set; } = Array.Empty<byte>();

        public string LinkedInProfile { get; set; } = string.Empty;

        public string FacebookProfile { get; set; } = string.Empty;

        public string InstagramProfile { get; set; } = string.Empty;

        public string XProfile { get; set; } = string.Empty;

        public EngineeringSpecialtyEnum Specialty { get; set; }

        public SkillLevelEnum SkillLevel { get; set; }

        public RoleEnum Role { get; set; }

        #endregion
    }
}
