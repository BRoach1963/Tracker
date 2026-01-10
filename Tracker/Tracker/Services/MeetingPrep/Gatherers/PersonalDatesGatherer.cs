using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers recognition opportunities like upcoming birthdays and work anniversaries.
    /// </summary>
    public class PersonalDatesGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Personal Dates Gatherer";
        public PrepSectionType SectionType => PrepSectionType.Recognition;
        public bool IsEnabled { get; set; } = true;

        public PersonalDatesGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("PersonalDatesGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.Recognition);
            var settings = GetSettings();

            try
            {
                var today = DateTime.Today;

                // Check for birthday
                if (teamMember.Birthday.HasValue && teamMember.Birthday.Value.Year > 1900)
                {
                    var nextBirthday = new DateTime(
                        today.Year,
                        teamMember.Birthday.Value.Month,
                        teamMember.Birthday.Value.Day
                    );

                    // If birthday has passed this year, look at next year
                    if (nextBirthday < today)
                        nextBirthday = nextBirthday.AddYears(1);

                    var daysUntil = (nextBirthday - today).Days;

                    if (daysUntil <= settings.BirthdayLookAheadDays)
                    {
                        section.Items.Add(new PrepItem
                        {
                            Title = daysUntil == 0
                                ? "🎂 Birthday TODAY!"
                                : $"🎂 Birthday in {daysUntil} day{(daysUntil != 1 ? "s" : "")}",
                            Subtext = nextBirthday.ToString("MMMM d"),
                            Priority = daysUntil == 0 ? PrepItemPriority.High : PrepItemPriority.Normal,
                            Icon = "Gift"
                        });
                    }
                }

                // Check for work anniversary
                if (teamMember.HireDate.HasValue && teamMember.HireDate.Value.Year > 1900)
                {
                    var nextAnniversary = new DateTime(
                        today.Year,
                        teamMember.HireDate.Value.Month,
                        teamMember.HireDate.Value.Day
                    );

                    // If anniversary has passed this year, look at next year
                    if (nextAnniversary < today)
                        nextAnniversary = nextAnniversary.AddYears(1);

                    var daysUntil = (nextAnniversary - today).Days;
                    var years = nextAnniversary.Year - teamMember.HireDate.Value.Year;

                    if (daysUntil <= settings.AnniversaryLookAheadDays)
                    {
                        var yearText = years == 1 ? "1 Year" : $"{years} Years";
                        section.Items.Add(new PrepItem
                        {
                            Title = daysUntil == 0
                                ? $"🎉 {yearText} Anniversary TODAY!"
                                : $"🎉 {yearText} Anniversary in {daysUntil} day{(daysUntil != 1 ? "s" : "")}",
                            Subtext = $"Joined {teamMember.HireDate.Value:MMMM d, yyyy}",
                            Priority = daysUntil == 0 ? PrepItemPriority.High : PrepItemPriority.Normal,
                            Icon = "Trophy"
                        });
                    }
                }

                // Add tenure info if no upcoming dates but a significant milestone
                if (!section.HasItems && teamMember.HireDate.HasValue && teamMember.HireDate.Value.Year > 1900)
                {
                    var tenure = (today - teamMember.HireDate.Value).Days / 365;
                    if (tenure > 0)
                    {
                        section.Items.Add(new PrepItem
                        {
                            Title = $"Tenure: {tenure} year{(tenure != 1 ? "s" : "")}",
                            Subtext = $"With the team since {teamMember.HireDate.Value:MMMM yyyy}",
                            Priority = PrepItemPriority.Low,
                            Icon = "Person"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering personal dates: {0}", ex.Message);
            }

            // Only return section if there are actual recognition opportunities
            var hasRecognitionOpportunity = section.Items.Any(i => i.Priority >= PrepItemPriority.Normal);
            return hasRecognitionOpportunity ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
