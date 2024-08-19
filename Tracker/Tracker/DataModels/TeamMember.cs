namespace Tracker.DataModels
{
    public class TeamMember
    {
        #region Ctor

        public TeamMember()
        {

        }

        #endregion

        #region Public Properties

        public int Id { get; set; }

        public string FirstName { get; set; } 

        public string LastName { get; set; }

        public string NickName { get; set; }    

        public string Email { get; set; }   

        public string CellPhone { get; set; } 

        public string JobTitle { get; set; }

        public DateTime BirthDay { get; set; } = DateTime.MinValue;

        public DateTime HireDate { get; set; } = new DateTime(1900, 1, 1);

        public DateTime TerminationDate { get; set; } = new DateTime(1900, 1, 1);

        public bool IsActive { get; set; } = true;

        public int ManagerId { get; set; } = 0;

        public byte[] ProfileImage { get; set; }

        public string LinkedInProfile { get; set; }

        #endregion
    }
}
