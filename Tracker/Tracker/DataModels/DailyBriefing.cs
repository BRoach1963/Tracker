using System;
using System.Collections.Generic;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a daily briefing generated for the manager.
    /// Contains all relevant information for the day at a glance.
    /// </summary>
    public class DailyBriefing
    {
        /// <summary>
        /// When this briefing was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Personalized greeting (e.g., "Good morning, Brian!").
        /// </summary>
        public string Greeting { get; set; } = string.Empty;

        /// <summary>
        /// Meetings scheduled for today.
        /// </summary>
        public List<Meeting> MeetingsToday { get; set; } = new();

        /// <summary>
        /// Critical insights requiring immediate attention.
        /// </summary>
        public List<Insight> CriticalInsights { get; set; } = new();

        /// <summary>
        /// Warning-level insights needing attention soon.
        /// </summary>
        public List<Insight> WarningInsights { get; set; } = new();

        /// <summary>
        /// Informational insights (FYI).
        /// </summary>
        public List<Insight> InfoInsights { get; set; } = new();

        /// <summary>
        /// Team members with birthdays in the next 7 days.
        /// </summary>
        public List<TeamMember> UpcomingBirthdays { get; set; } = new();

        /// <summary>
        /// Team members with work anniversaries in the next 7 days.
        /// </summary>
        public List<TeamMember> UpcomingAnniversaries { get; set; } = new();

        /// <summary>
        /// Count of active Goals.
        /// </summary>
        public int ActiveGoalCount { get; set; }

        /// <summary>
        /// Count of Goals currently on track.
        /// </summary>
        public int GoalsOnTrack { get; set; }

        /// <summary>
        /// Count of Goals at risk of missing target.
        /// </summary>
        public int GoalsAtRisk { get; set; }

        /// <summary>
        /// Count of overdue tasks.
        /// </summary>
        public int OverdueTaskCount { get; set; }

        /// <summary>
        /// Count of stale action items.
        /// </summary>
        public int StaleActionItemCount { get; set; }

        /// <summary>
        /// Optional AI-generated summary of the day.
        /// </summary>
        public string? AiSummary { get; set; }

        /// <summary>
        /// Total count of all insights.
        /// </summary>
        public int TotalInsightCount =>
            CriticalInsights.Count + WarningInsights.Count + InfoInsights.Count;

        /// <summary>
        /// Whether there are any critical items needing attention.
        /// </summary>
        public bool HasCriticalItems => CriticalInsights.Count > 0;

        /// <summary>
        /// Whether there are any meetings scheduled today.
        /// </summary>
        public bool HasMeetingsToday => MeetingsToday.Count > 0;

        /// <summary>
        /// Generates an appropriate time-based greeting.
        /// </summary>
        public static string GetGreeting(string userName)
        {
            var hour = DateTime.Now.Hour;
            var timeGreeting = hour switch
            {
                < 12 => "Good morning",
                < 17 => "Good afternoon",
                _ => "Good evening"
            };

            return string.IsNullOrEmpty(userName)
                ? $"{timeGreeting}!"
                : $"{timeGreeting}, {userName}!";
        }
    }
}
