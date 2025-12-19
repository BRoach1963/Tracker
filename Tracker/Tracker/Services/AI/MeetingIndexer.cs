using System.Text;
using Tracker.Database;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes 1:1 meetings for semantic search
    /// </summary>
    public class MeetingIndexer : EntityIndexerBase
    {
        private static readonly Lazy<MeetingIndexer> _instance = 
            new(() => new MeetingIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static MeetingIndexer Instance => _instance.Value;

        private MeetingIndexer() : base("MeetingIndexer")
        {
        }

        /// <summary>
        /// Indexes all 1:1 meetings as searchable vectors (incremental if sinceTime provided)
        /// </summary>
        /// <param name="sinceTime">Only index meetings created/modified after this time (null = all)</param>
        public async Task<int> IndexAllAsync(DateTime? sinceTime = null)
        {
            ResetCount();
            if (sinceTime == null)
                _logger.Info("Starting full meeting indexing...");
            else
                _logger.Info("Starting incremental meeting indexing since {0}...", sinceTime.Value.ToString("g"));

            try
            {
                var meetings = await TrackerDbManager.Instance.GetOneOnOnesAsync();
                var activeMeetings = meetings.Where(m => !m.IsDeleted).ToList();

                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    activeMeetings = activeMeetings
                        .Where(m => m.CreatedAt > sinceTime.Value || m.LastModifiedAt > sinceTime.Value)
                        .ToList();
                }

                foreach (var meeting in activeMeetings)
                {
                    await IndexMeetingAsync(meeting);
                }

                _logger.Info("Indexed {0} meetings", _indexedCount);
                return _indexedCount;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error indexing meetings");
                return _indexedCount;
            }
        }

        private async Task IndexMeetingAsync(DataModels.OneOnOne meeting)
        {
            try
            {
                // Create rich text representation
                var sb = new StringBuilder();
                sb.AppendLine($"1:1 Meeting with {meeting.TeamMember?.FullName ?? "Unknown"}");
                sb.AppendLine($"Date: {meeting.Date:MMMM d, yyyy}");
                sb.AppendLine($"Status: {meeting.Status}");
                
                if (!string.IsNullOrEmpty(meeting.Notes))
                {
                    sb.AppendLine($"Notes: {meeting.Notes}");
                }

                // Include agenda items
                if (meeting.AgendaItems?.Any() == true)
                {
                    sb.AppendLine($"Agenda Items ({meeting.AgendaItems.Count}):");
                    foreach (var item in meeting.AgendaItems.Take(10))
                    {
                        sb.AppendLine($"  - {item.Description}");
                        if (!string.IsNullOrEmpty(item.Resolution))
                            sb.AppendLine($"    Resolution: {item.Resolution}");
                    }
                }

                var content = sb.ToString();

                // Metadata for filtering
                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "meeting",
                    ["id"] = meeting.Id,
                    ["team_member_name"] = meeting.TeamMember?.FullName ?? "Unknown",
                    ["date"] = meeting.Date.ToString("yyyy-MM-dd"),
                    ["status"] = meeting.Status.ToString()
                };

                await IndexEntityAsync($"meeting_{meeting.Id}", content, metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing meeting {0}: {1}", meeting.Id, ex.Message);
            }
        }
    }
}
