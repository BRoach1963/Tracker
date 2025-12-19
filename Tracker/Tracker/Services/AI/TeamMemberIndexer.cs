using System.Text;
using Tracker.Database;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes team members for semantic search
    /// </summary>
    public class TeamMemberIndexer : EntityIndexerBase
    {
        private static readonly Lazy<TeamMemberIndexer> _instance = 
            new(() => new TeamMemberIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TeamMemberIndexer Instance => _instance.Value;

        private TeamMemberIndexer() : base("TeamMemberIndexer")
        {
        }

        /// <summary>
        /// Indexes all team members as searchable vectors (incremental if sinceTime provided)
        /// </summary>
        /// <param name="sinceTime">Only index members created/modified after this time (null = all)</param>
        public async Task<int> IndexAllAsync(DateTime? sinceTime = null)
        {
            ResetCount();
            if (sinceTime == null)
                _logger.Info("Starting full team member indexing...");
            else
                _logger.Info("Starting incremental team member indexing since {0}...", sinceTime.Value.ToString("g"));

            try
            {
                var members = await TrackerDbManager.Instance.GetTeamMembersAsync();
                var activeMembers = members.Where(m => !m.IsDeleted).ToList();

                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    activeMembers = activeMembers
                        .Where(m => m.CreatedAt > sinceTime.Value || m.LastModifiedAt > sinceTime.Value)
                        .ToList();
                }

                foreach (var member in activeMembers)
                {
                    await IndexTeamMemberAsync(member);
                }

                _logger.Info("Indexed {0} team members", _indexedCount);
                return _indexedCount;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error indexing team members");
                return _indexedCount;
            }
        }

        private async Task IndexTeamMemberAsync(DataModels.TeamMember member)
        {
            try
            {
                // Create rich text representation
                var sb = new StringBuilder();
                sb.AppendLine($"{member.FullName}");
                sb.AppendLine($"Job Title: {member.JobTitle}");
                
                if (member.HireDate != default && member.HireDate != DateTime.MinValue)
                    sb.AppendLine($"Hire Date: {member.HireDate:MMMM d, yyyy}");
                
                if (member.BirthDay != default && member.BirthDay != DateTime.MinValue)
                    sb.AppendLine($"Birthday: {member.BirthDay:MMMM d}");
                
                if (!string.IsNullOrEmpty(member.Email))
                    sb.AppendLine($"Email: {member.Email}");
                
                if (!string.IsNullOrEmpty(member.CellPhone))
                    sb.AppendLine($"Phone: {member.CellPhone}");
                
                if (!string.IsNullOrEmpty(member.NickName))
                    sb.AppendLine($"Nickname: {member.NickName}");
                
                sb.AppendLine($"Status: {(member.IsActive ? "Active" : "Inactive")}");
                
                if (!member.IsActive && member.TerminationDate != default)
                    sb.AppendLine($"Termination Date: {member.TerminationDate:MMMM d, yyyy}");

                var content = sb.ToString();

                // Metadata for filtering
                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "team_member",
                    ["id"] = member.Id,
                    ["name"] = member.FullName,
                    ["is_active"] = member.IsActive
                };

                await IndexEntityAsync($"team_member_{member.Id}", content, metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing team member {0}: {1}", member.Id, ex.Message);
            }
        }
    }
}
