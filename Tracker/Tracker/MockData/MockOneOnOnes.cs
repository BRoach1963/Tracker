using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.MockData
{
    public static class MockOneOnOnes
    {
        public static ObservableCollection<OneOnOne> GetMockOneOnOneData(List<TeamMember> teamMembers)
        {
            if (teamMembers.Count == 0) return new ObservableCollection<OneOnOne>();
            return
                new ObservableCollection<OneOnOne>
                {
                    new OneOnOne
                    {
                        Id = 1,
                        Date = DateTime.Now.AddDays(7),
                        StartTime = new TimeSpan(10, 0, 0),
                        Duration = TimeSpan.FromHours(1),
                        Agenda = "Project updates and performance review",
                        Notes = "Discussed progress on current project and provided feedback.",
                        ActionItems =
                            new List<ActionItem>
                            {
                                new ActionItem
                                {
                                    Description = "Complete project milestone 2", DueDate = DateTime.Now.AddDays(7),
                                    IsCompleted = false
                                },
                                new ActionItem
                                {
                                    Description = "Prepare for upcoming sprint review",
                                    DueDate = DateTime.Now.AddDays(3),
                                    IsCompleted = false
                                }
                            },
                        IsRecurring = true,
                        DiscussionPoints = new List<DiscussionPoint> {
                            new DiscussionPoint()
                            {
                                Description = "Project progress"
                            },
                            new DiscussionPoint()
                                { Description = "Team collaboration" },
                            new DiscussionPoint()
                                { Description = "Performance review" }
                        }, 
                        Feedback = "Great progress, keep up the good work.",
                        Concerns = new List<Concern> { new Concern()
                        {
                            Description = "Need more resources for the project."
                        } }, 
                        ObjectiveKeyResults =
                            new List<ObjectiveKeyResult>
                            {
                                new ObjectiveKeyResult
                                    { Description = "Improve code quality", EndDate = DateTime.Now.AddMonths(1) },

                                new ObjectiveKeyResult
                                {
                                    Description = "Increase test coverage by 20%", EndDate = DateTime.Now.AddMonths(2)
                                }
                            },
                        FollowUpItems =
                            new List<FollowUpItem>
                            {
                                new FollowUpItem
                                {
                                    Description = "Review code quality metrics", DueDate = DateTime.Now.AddDays(14),
                                    IsCompleted = false
                                }
                            },
                        Status = MeetingStatusEnum.Completed,
                        TeamMember = teamMembers[0] // Assigning the first team member
                    },

                    new OneOnOne
                    {
                        Id = 2,
                        Date = DateTime.Now.AddDays(6),
                        StartTime = new TimeSpan(14, 30, 0),
                        Duration = TimeSpan.FromMinutes(45),
                        Agenda = "Career development discussion",
                        Notes = "Talked about career goals and opportunities for growth.",
                        ActionItems =
                            new List<ActionItem>
                            {
                                new ActionItem
                                {
                                    Description = "Research courses for skill development",
                                    DueDate = DateTime.Now.AddDays(10),
                                    IsCompleted = true
                                }
                            },
                        IsRecurring = false,
                        DiscussionPoints = new List<DiscussionPoint> {
                            new DiscussionPoint()
                            {
                                Description = "Career growth"
                            },
                            new DiscussionPoint()
                                { Description = "Skill development" },
                            new DiscussionPoint()
                                { Description = "Performance review" }
                        }, 
                        Feedback = "Consider taking advanced courses in data science.",
                        Concerns = new List<Concern> { new Concern()
                        {
                            Description = "Uncertainty about career path"
                        } }, 
                        ObjectiveKeyResults =
                            new List<ObjectiveKeyResult>
                            {
                                new ObjectiveKeyResult
                                {
                                    Description = "Complete advanced data science course",
                                    EndDate = DateTime.Now.AddMonths(3)
                                }
                            },
                        FollowUpItems =
                            new List<FollowUpItem>
                            {
                                new FollowUpItem
                                {
                                    Description = "Check progress on data science course",
                                    DueDate = DateTime.Now.AddDays(30),
                                    IsCompleted = false
                                }
                            },
                        Status = MeetingStatusEnum.Scheduled,
                        TeamMember = teamMembers[1] // Assigning the second team member
                    },

                    new OneOnOne
                    {
                        Id = 3,
                        Date = DateTime.Now.AddDays(8),
                        StartTime = new TimeSpan(9, 15, 0),
                        Duration = TimeSpan.FromMinutes(30),
                        Agenda = "Work-life balance and workload discussion",
                        Notes =
                            "Discussed the current workload and strategies to maintain a healthy work-life balance.",
                        ActionItems =
                            new List<ActionItem>
                            {
                                new ActionItem
                                {
                                    Description = "Delegate tasks to team members", DueDate = DateTime.Now.AddDays(5),
                                    IsCompleted = false
                                }
                            },
                        IsRecurring = true,
                        DiscussionPoints = new List<DiscussionPoint> {
                            new DiscussionPoint()
                        {
                            Description = "Workload management"
                        },
                            new DiscussionPoint()
                                { Description = "Work-life balance" }
                            },
                        Feedback = "Ensure to take regular breaks and avoid burnout.",
                        Concerns = new List<Concern> { new Concern()
                        {
                            Description = "Overwhelmed with current tasks."
                        } },
                        ObjectiveKeyResults =
                            new List<ObjectiveKeyResult>
                            {
                                new ObjectiveKeyResult
                                {
                                    Description = "Improve time management skills", EndDate = DateTime.Now.AddMonths(2)
                                }
                            },
                        FollowUpItems =
                            new List<FollowUpItem>
                            {
                                new FollowUpItem
                                {
                                    Description = "Review task delegation", DueDate = DateTime.Now.AddDays(10),
                                    IsCompleted = false
                                }
                            },
                        Status = MeetingStatusEnum.Rescheduled,
                        TeamMember = teamMembers[2] // Assigning the third team member
                    }
                };
        }
    }
}
