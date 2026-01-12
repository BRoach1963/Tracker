using System.Text;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;

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

        protected override string EntityTypeName => "meetings";

        protected override async Task<IEnumerable<object>> FetchEntitiesAsync()
        {
            var meetings = await TrackerDataManager.Instance.GetOneOnOneMeetings();
            return meetings.Where(m => !m.IsDeleted).Cast<object>();
        }

        protected override async Task IndexSingleEntityAsync(object entity)
        {
            var meeting = (DataModels.Meeting)entity;
            try
            {
                // Create rich text representation
                var sb = new StringBuilder();
                sb.AppendLine($"1:1 Meeting with {meeting.Report?.FullName ?? "Unknown"}");
                sb.AppendLine($"Date: {meeting.ScheduledAt:MMMM d, yyyy}");
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
                        sb.AppendLine($"  - {item.Title}");
                        if (!string.IsNullOrEmpty(item.Notes))
                            sb.AppendLine($"    Notes: {item.Notes}");
                    }
                }

                var content = sb.ToString();

                // Metadata for filtering
                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "meeting",
                    ["id"] = meeting.Id,
                    ["team_member_name"] = meeting.Report?.FullName ?? "Unknown",
                    ["date"] = meeting.ScheduledAt.ToString("yyyy-MM-dd"),
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
