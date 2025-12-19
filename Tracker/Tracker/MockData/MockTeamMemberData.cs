using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    /// <summary>
    /// Mock team member data for UI previews and testing.
    /// This data matches the DatabaseSeeder's sample team for consistency.
    /// </summary>
    public static class MockTeamMemberData
    {
        public static ObservableCollection<TeamMember> GetMockTeamMembers()
        {
            return new ObservableCollection<TeamMember>
            {
                // Manager/Tech Lead
                new TeamMember
                {
                    Id = 1,
                    FirstName = "Alex",
                    LastName = "Rivera",
                    NickName = "Alex",
                    Email = "alex.rivera@techcorp.com",
                    CellPhone = "555-100-0001",
                    JobTitle = "Engineering Manager",
                    BirthDay = new DateTime(1985, 6, 15),
                    HireDate = new DateTime(2020, 3, 1),
                    IsActive = true,
                    ManagerId = 0,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Principle,
                    Role = RoleEnum.Manager
                },
                // Senior Backend Engineer
                new TeamMember
                {
                    Id = 2,
                    FirstName = "Jordan",
                    LastName = "Chen",
                    Email = "jordan.chen@techcorp.com",
                    CellPhone = "555-100-0002",
                    JobTitle = "Senior Backend Engineer",
                    BirthDay = new DateTime(1988, 11, 22),
                    HireDate = new DateTime(2021, 6, 15),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Senior Frontend Engineer
                new TeamMember
                {
                    Id = 3,
                    FirstName = "Morgan",
                    LastName = "Patel",
                    Email = "morgan.patel@techcorp.com",
                    CellPhone = "555-100-0003",
                    JobTitle = "Senior Frontend Engineer",
                    BirthDay = new DateTime(1990, 3, 8),
                    HireDate = new DateTime(2022, 1, 10),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Mid-level Full Stack
                new TeamMember
                {
                    Id = 4,
                    FirstName = "Taylor",
                    LastName = "Kim",
                    Email = "taylor.kim@techcorp.com",
                    CellPhone = "555-100-0004",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1993, 7, 14),
                    HireDate = new DateTime(2023, 2, 20),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                // Mid-level Backend
                new TeamMember
                {
                    Id = 5,
                    FirstName = "Casey",
                    LastName = "Okonkwo",
                    Email = "casey.okonkwo@techcorp.com",
                    CellPhone = "555-100-0005",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1994, 12, 3),
                    HireDate = new DateTime(2023, 8, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                // Senior Data Engineer
                new TeamMember
                {
                    Id = 6,
                    FirstName = "Riley",
                    LastName = "Nakamura",
                    Email = "riley.nakamura@techcorp.com",
                    CellPhone = "555-100-0006",
                    JobTitle = "Senior Data Engineer",
                    BirthDay = new DateTime(1987, 9, 28),
                    HireDate = new DateTime(2021, 11, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.DataScience,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Junior Frontend
                new TeamMember
                {
                    Id = 7,
                    FirstName = "Jamie",
                    LastName = "Santos",
                    Email = "jamie.santos@techcorp.com",
                    CellPhone = "555-100-0007",
                    JobTitle = "Junior Software Engineer",
                    BirthDay = new DateTime(1998, 4, 19),
                    HireDate = new DateTime(2024, 6, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Junior,
                    Role = RoleEnum.Engineer
                }
            };
        }
    }
}
