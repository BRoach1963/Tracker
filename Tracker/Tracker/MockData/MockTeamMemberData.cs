using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    public static class MockTeamMemberData
    {
        public static ObservableCollection<TeamMember> GetMockTeamMemberData()
        {
            return new ObservableCollection<TeamMember>
            {
                new TeamMember
                {
                    Id = 1,
                    FirstName = "Terry",
                    LastName = "Bradshaw",
                    Email = "terry.bradshaw@gmail.com",
                    CellPhone = "555-1234",
                    JobTitle = "Quarterback",
                    BirthDay = new DateTime(1948, 9, 2),
                    HireDate = new DateTime(1970, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/terry-bradshaw",
                    FacebookProfile = "https://www.facebook.com/terry.bradshaw",
                    InstagramProfile = "https://www.instagram.com/terry.bradshaw",
                    XProfile = "https://twitter.com/terrybradshaw",
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Principal
                },
                new TeamMember
                {
                    Id = 2,
                    FirstName = "Franco",
                    LastName = "Harris",
                    Email = "franco.harris@gmail.com",
                    CellPhone = "555-5678",
                    JobTitle = "Running Back",
                    BirthDay = new DateTime(1950, 3, 7),
                    HireDate = new DateTime(1972, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/franco-harris",
                    FacebookProfile = "https://www.facebook.com/franco.harris",
                    InstagramProfile = "https://www.instagram.com/franco.harris",
                    XProfile = "https://twitter.com/francoharris",
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                new TeamMember
                {
                    Id = 3,
                    FirstName = "Joe",
                    LastName = "Greene",
                    Email = "joe.greene@gmail.com",
                    CellPhone = "555-8765",
                    JobTitle = "Defensive Tackle",
                    BirthDay = new DateTime(1946, 9, 24),
                    HireDate = new DateTime(1969, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/joe-greene",
                    FacebookProfile = "https://www.facebook.com/joe.greene",
                    InstagramProfile = "https://www.instagram.com/joe.greene",
                    XProfile = "https://twitter.com/joegreene",
                    Specialty = EngineeringSpecialtyEnum.DataScience,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Architect
                },
                new TeamMember
                {
                    Id = 4,
                    FirstName = "Lynn",
                    LastName = "Swann",
                    Email = "lynn.swann@gmail.com",
                    CellPhone = "555-4321",
                    JobTitle = "Wide Receiver",
                    BirthDay = new DateTime(1952, 3, 7),
                    HireDate = new DateTime(1974, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/lynn-swann",
                    FacebookProfile = "https://www.facebook.com/lynn.swann",
                    InstagramProfile = "https://www.instagram.com/lynn.swann",
                    XProfile = "https://twitter.com/lynnswann",
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                new TeamMember
                {
                    Id = 5,
                    FirstName = "Jack",
                    LastName = "Lambert",
                    Email = "jack.lambert@gmail.com",
                    CellPhone = "555-8765",
                    JobTitle = "Linebacker",
                    BirthDay = new DateTime(1952, 7, 8),
                    HireDate = new DateTime(1974, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/jack-lambert",
                    FacebookProfile = "https://www.facebook.com/jack.lambert",
                    InstagramProfile = "https://www.instagram.com/jack.lambert",
                    XProfile = "https://twitter.com/jacklambert",
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Principal
                },
                new TeamMember
                {
                    Id = 6,
                    FirstName = "Mel",
                    LastName = "Blount",
                    Email = "mel.blount@gmail.com",
                    CellPhone = "555-6543",
                    JobTitle = "Cornerback",
                    BirthDay = new DateTime(1948, 4, 10),
                    HireDate = new DateTime(1970, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/mel-blount",
                    FacebookProfile = "https://www.facebook.com/mel.blount",
                    InstagramProfile = "https://www.instagram.com/mel.blount",
                    XProfile = "https://twitter.com/melblount",
                    Specialty = EngineeringSpecialtyEnum.AI,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                new TeamMember
                {
                    Id = 7,
                    FirstName = "John",
                    LastName = "Stallworth",
                    Email = "john.stallworth@gmail.com",
                    CellPhone = "555-1234",
                    JobTitle = "Wide Receiver",
                    BirthDay = new DateTime(1952, 7, 15),
                    HireDate = new DateTime(1974, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/john-stallworth",
                    FacebookProfile = "https://www.facebook.com/john.stallworth",
                    InstagramProfile = "https://www.instagram.com/john.stallworth",
                    XProfile = "https://twitter.com/johnstallworth",
                    Specialty = EngineeringSpecialtyEnum.iOS,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                new TeamMember
                {
                    Id = 8,
                    FirstName = "Jack",
                    LastName = "Ham",
                    Email = "jack.ham@gmail.com",
                    CellPhone = "555-5678",
                    JobTitle = "Linebacker",
                    BirthDay = new DateTime(1948, 12, 23),
                    HireDate = new DateTime(1971, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/jack-ham",
                    FacebookProfile = "https://www.facebook.com/jack.ham",
                    InstagramProfile = "https://www.instagram.com/jack.ham",
                    XProfile = "https://twitter.com/jackham",
                    Specialty = EngineeringSpecialtyEnum.Windows,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                new TeamMember
                {
                    Id = 9,
                    FirstName = "Rod",
                    LastName = "Woodson",
                    Email = "rod.woodson@gmail.com",
                    CellPhone = "555-8765",
                    JobTitle = "Cornerback",
                    BirthDay = new DateTime(1965, 3, 10),
                    HireDate = new DateTime(1987, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/rod-woodson",
                    FacebookProfile = "https://www.facebook.com/rod.woodson",
                    InstagramProfile = "https://www.instagram.com/rod.woodson",
                    XProfile = "https://twitter.com/rodwoodson",
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Principal
                },
                new TeamMember
                {
                    Id = 10,
                    FirstName = "Mike",
                    LastName = "Webster",
                    Email = "mike.webster@gmail.com",
                    CellPhone = "555-4321",
                    JobTitle = "Center",
                    BirthDay = new DateTime(1952, 3, 18),
                    HireDate = new DateTime(1974, 9, 1),
                    IsActive = true,
                    ManagerId = 0,
                    LinkedInProfile = "https://www.linkedin.com/in/mike-webster",
                    FacebookProfile = "https://www.facebook.com/mike.webster",
                    InstagramProfile = "https://www.instagram.com/mike.webster",
                    XProfile = "https://twitter.com/mikewebster",
                    Specialty = EngineeringSpecialtyEnum.DataScience,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer

                }

            };
        }
    }
}