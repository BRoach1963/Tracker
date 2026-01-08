using System.Text;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;

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

        protected override string EntityTypeName => "team members";

        protected override async Task<IEnumerable<object>> FetchEntitiesAsync()
        {
            var members = await TrackerDataManager.Instance.GetTeamData();
            return members.Where(m => !m.IsDeleted).Cast<object>();
        }

        protected override async Task IndexSingleEntityAsync(object entity)
        {
            var member = (DataModels.TeamMember)entity;
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
