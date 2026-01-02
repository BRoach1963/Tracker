using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes upcoming personal dates (birthdays and work anniversaries)
    /// and generates insights to help managers recognize their team.
    /// </summary>
    public class PersonalDateAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "Personal Date Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[]
        {
            InsightType.UpcomingBirthday,
            InsightType.UpcomingAnniversary
        };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Days ahead to look for upcoming birthdays.
        /// </summary>
        public int BirthdayLookAheadDays { get; set; } = 7;

        /// <summary>
        /// Days ahead to look for upcoming work anniversaries.
        /// </summary>
        public int AnniversaryLookAheadDays { get; set; } = 7;

        public PersonalDateAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("PersonalDateAnalyzer");

            // Load thresholds from settings if available
            var settings = UserSettingsManager.Instance?.Settings?.Insights;
            if (settings != null)
            {
                BirthdayLookAheadDays = settings.BirthdayLookAheadDays;
                AnniversaryLookAheadDays = settings.AnniversaryLookAheadDays;
            }
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping personal date analysis");
                    return insights;
                }

                // Get all active team members
                var teamMembers = await dbManager.GetTeamMembersAsync();
                if (teamMembers == null || teamMembers.Count == 0)
                {
                    _logger.Debug("No team members found");
                    return insights;
                }

                var today = DateTime.Now.Date;

                foreach (var member in teamMembers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Skip inactive team members
                    if (!member.IsActive)
                        continue;

                    // Check birthday
                    CheckBirthday(member, today, insights);

                    // Check work anniversary
                    CheckAnniversary(member, today, insights);
                }

                _logger.Info("Personal date analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during personal date analysis");
            }

            return insights;
        }

        private void CheckBirthday(TeamMember member, DateTime today, List<Insight> insights)
        {
            // Skip if no birthday set (Year < 1901 means not set)
            if (member.BirthDay.Year < 1901)
                return;

            // Get this year's birthday
            var thisYearBirthday = new DateTime(today.Year, member.BirthDay.Month, member.BirthDay.Day);

            // If birthday already passed this year, check next year
            if (thisYearBirthday < today)
            {
                thisYearBirthday = thisYearBirthday.AddYears(1);
            }

            var daysUntilBirthday = (thisYearBirthday - today).Days;

            if (daysUntilBirthday <= BirthdayLookAheadDays)
            {
                var age = thisYearBirthday.Year - member.BirthDay.Year;
                var dayText = daysUntilBirthday == 0 ? "today" :
                              daysUntilBirthday == 1 ? "tomorrow" :
                              $"in {daysUntilBirthday} days";

                insights.Add(new Insight
                {
                    UniqueKey = $"birthday_{member.Id}_{thisYearBirthday:yyyy-MM-dd}",
                    Type = InsightType.UpcomingBirthday,
                    Severity = daysUntilBirthday <= 1 ? InsightSeverity.Warning : InsightSeverity.Info,
                    Title = $"🎂 {member.FirstName}'s birthday is {dayText}",
                    Description = $"{member.FullName} is turning {age} on {thisYearBirthday:MMMM d}. Consider sending a birthday message or recognition!",
                    ActionSuggestion = "Send Kudos",
                    EntityType = "TeamMember",
                    EntityId = member.Id,
                    GeneratedAt = DateTime.Now
                });
            }
        }

        private void CheckAnniversary(TeamMember member, DateTime today, List<Insight> insights)
        {
            // Skip if no hire date set (Year < 1901 means not set)
            if (member.HireDate.Year < 1901)
                return;

            // Get this year's anniversary
            var thisYearAnniversary = new DateTime(today.Year, member.HireDate.Month, member.HireDate.Day);

            // If anniversary already passed this year, check next year
            if (thisYearAnniversary < today)
            {
                thisYearAnniversary = thisYearAnniversary.AddYears(1);
            }

            var daysUntilAnniversary = (thisYearAnniversary - today).Days;

            if (daysUntilAnniversary <= AnniversaryLookAheadDays)
            {
                var yearsAtCompany = thisYearAnniversary.Year - member.HireDate.Year;
                var dayText = daysUntilAnniversary == 0 ? "today" :
                              daysUntilAnniversary == 1 ? "tomorrow" :
                              $"in {daysUntilAnniversary} days";

                // Only highlight significant anniversaries (1, 5, 10, 15, 20, etc.)
                var isMilestone = yearsAtCompany == 1 || yearsAtCompany % 5 == 0;

                insights.Add(new Insight
                {
                    UniqueKey = $"anniversary_{member.Id}_{thisYearAnniversary:yyyy-MM-dd}",
                    Type = InsightType.UpcomingAnniversary,
                    Severity = isMilestone ? InsightSeverity.Warning : InsightSeverity.Info,
                    Title = $"🎉 {member.FirstName}'s {yearsAtCompany}-year anniversary is {dayText}",
                    Description = $"{member.FullName} will celebrate {yearsAtCompany} year{(yearsAtCompany != 1 ? "s" : "")} with the company on {thisYearAnniversary:MMMM d}. {(isMilestone ? "This is a milestone worth celebrating!" : "Consider acknowledging their dedication.")}",
                    ActionSuggestion = "Send Kudos",
                    EntityType = "TeamMember",
                    EntityId = member.Id,
                    GeneratedAt = DateTime.Now
                });
            }
        }
    }
}
