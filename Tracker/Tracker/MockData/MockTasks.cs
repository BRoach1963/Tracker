using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.MockData
{
    public static  class MockTasks
    {
        public static ObservableCollection<ITask> GetMockTaskData()
        {
            var oneOnOnes = MockOneOnOnes.GetMockOneOnOneData();

            var tasks = new ObservableCollection<ITask>();

            foreach (var oneOnOne in oneOnOnes)
            {
                // Add all ActionItems
                foreach (var actionItem in oneOnOne.ActionItems)
                {
                    actionItem.Owner = oneOnOne.TeamMember;
                    tasks.Add(actionItem);
                }

                // Add all FollowUpItems
                foreach (var followUpItem in oneOnOne.FollowUpItems)
                {
                    followUpItem.Owner = oneOnOne.TeamMember;
                    tasks.Add(followUpItem);
                }
               
            }

            tasks.Add(new IndividualTask
            {
                Description = "Prepare quarterly report",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(10),
                Notes = "Ensure all departments have submitted their data",
                Owner = new TeamMember
                {
                    FirstName = "Brian",
                    LastName = "Roach",
                    Role = RoleEnum.Manager,
                    Email = "brian.roach@tracker.com",
                    CellPhone = "609-605-5121"
                }
            });

            tasks.Add(new IndividualTask
            {
                Description = "Review budget allocations",
                IsCompleted = true,
                DueDate = DateTime.Now.AddDays(-2),
                Notes = "Finalize after discussion with the finance team",
                Owner = new TeamMember
                {
                    FirstName = "Brian",
                    LastName = "Roach",
                    Role = RoleEnum.Manager,
                    Email = "brian.roach@tracker.com",
                    CellPhone = "609-605-5121"
                }
            });

            tasks.Add(new IndividualTask
            {
                Description = "Organize team building activity",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(30),
                Notes = "Book venue and confirm participant list",
                Owner = new TeamMember
                {
                    FirstName = "Brian",
                    LastName = "Roach",
                    Role = RoleEnum.Manager,
                    Email = "brian.roach@tracker.com",
                    CellPhone = "609-605-5121"
                }
            });

            return tasks;
        }
    }
}
