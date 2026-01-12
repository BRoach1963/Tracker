using System.Text;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes tasks for semantic search
    /// </summary>
    public class TaskIndexer : EntityIndexerBase
    {
        private static readonly Lazy<TaskIndexer> _instance = 
            new(() => new TaskIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TaskIndexer Instance => _instance.Value;

        private TaskIndexer() : base("TaskIndexer")
        {
        }

        protected override string EntityTypeName => "tasks";

        protected override async Task<IEnumerable<object>> FetchEntitiesAsync()
        {
            var tasks = await TrackerDataManager.Instance.GetTasks();
            return tasks.Where(t => !t.IsDeleted).Cast<object>();
        }

        protected override async Task IndexSingleEntityAsync(object entity)
        {
            var task = (DataModels.TrackerTask)entity;
            try
            {
                // Create rich text representation
                var sb = new StringBuilder();
                sb.AppendLine($"Task: {task.Description}");
                
                if (task.Owner != null && !string.IsNullOrEmpty(task.Owner.FullName))
                    sb.AppendLine($"Owner: {task.Owner.FullName}");
                
                if (task.DueDate != default)
                    sb.AppendLine($"Due Date: {task.DueDate:MMMM d, yyyy}");
                
                sb.AppendLine($"Status: {(task.IsCompleted ? "Completed" : "Active")}");
                
                if (task.Project != null)
                    sb.AppendLine($"Project: {task.Project.Name}");
                
                if (!string.IsNullOrEmpty(task.Notes))
                    sb.AppendLine($"Notes: {task.Notes}");

                var content = sb.ToString();

                // Metadata for filtering
                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "task",
                    ["id"] = task.Id,
                    ["is_completed"] = task.IsCompleted
                };

                if (task.Owner != null)
                    metadata["owner_id"] = task.Owner.Id;

                if (task.ProjectId.HasValue)
                    metadata["project_id"] = task.ProjectId.Value;

                await IndexEntityAsync($"task_{task.Id}", content, metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing task {0}: {1}", task.Id, ex.Message);
            }
        }
    }
}
