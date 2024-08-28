using System.Collections.ObjectModel;
using Tracker.DataModels;

namespace Tracker.MockData
{
    public static class MockOkRs
    {
        public static ObservableCollection<ObjectiveKeyResult> GetMockOkrData()
        {
            var teamMembers = MockTeamMemberData.GetMockTeamMemberData();
            var kpis = MockKpIs.GetMockKpiData();
            var random = new Random();
            DateTime today = DateTime.Today;

            return
            [
                new ObjectiveKeyResult
                {
                    ObjectiveId = 1,
                    ProjectId = 1,
                    Title = "Improve Code Quality",
                    Description =
                        "Enhance the overall quality of the codebase by increasing code coverage and fixing bugs.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 1).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 2,
                    ProjectId = 1,
                    Title = "Increase Feature Delivery Rate",
                    Description = "Accelerate the delivery of features and improve review times.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 2).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 3,
                    ProjectId = 1,
                    Title = "Optimize Deployment and Lead Time",
                    Description = "Enhance the frequency of deployments and reduce lead time for changes.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 3).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 4,
                    ProjectId = 2,
                    Title = "Ensure System Reliability and Customer Satisfaction",
                    Description = "Maintain high system uptime and improve customer satisfaction ratings.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 4).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 5,
                    ProjectId = 2,
                    Title = "Enhance Security and Code Quality",
                    Description = "Reduce security vulnerabilities and improve code quality.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 5).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 6,
                    ProjectId = 3,
                    Title = "Improve Story Completion and Incident Resolution",
                    Description =
                        "Increase the completion rate of user stories and reduce the time to resolve incidents.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 6).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 7,
                    ProjectId = 4,
                    Title = "Reduce Technical Debt and Increase Automation",
                    Description = "Lower technical debt and increase automation coverage.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 7).ToList()
                },

                new ObjectiveKeyResult
                {
                    ObjectiveId = 8,
                    ProjectId = 4,
                    Title = "Enhance Server Performance",
                    Description = "Improve server response time and overall performance.",
                    Owner = teamMembers[random.Next(teamMembers.Count)],
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddMonths(3),
                    KeyResults = kpis.Where(kpi => kpi.OkrId == 8).ToList()
                }
            ];
        }
    }
}
