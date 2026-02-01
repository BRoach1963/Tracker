using System;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// DTO for the procohere.get_weekly_meeting_load RPC response.
/// Returns meeting count per day for a team member.
/// </summary>
public class WeeklyMeetingLoadResult
{
    /// <summary>
    /// The date of the meetings.
    /// </summary>
    [JsonPropertyName("meeting_date")]
    public DateTime MeetingDate { get; set; }

    /// <summary>
    /// Number of meetings on this date.
    /// </summary>
    [JsonPropertyName("meeting_count")]
    public int MeetingCount { get; set; }
}
