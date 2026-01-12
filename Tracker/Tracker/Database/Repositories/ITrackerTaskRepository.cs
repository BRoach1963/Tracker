using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for TrackerTask operations (formerly IndividualTask).
    /// Handles all data access for work tasks, action items, and subtasks.
    /// </summary>
    public interface ITrackerTaskRepository
    {
        /// <summary>
        /// Retrieves all tasks for the current user.
        /// </summary>
        Task<List<TrackerTask>> GetTasksAsync();

        /// <summary>
        /// Retrieves a specific task by ID.
        /// </summary>
        Task<TrackerTask?> GetTaskByIdAsync(Guid id);

        /// <summary>
        /// Adds a new task.
        /// </summary>
        Task<Guid> AddTaskAsync(TrackerTask task);

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        Task<bool> UpdateTaskAsync(TrackerTask task);

        /// <summary>
        /// Deletes a task by ID.
        /// </summary>
        Task<bool> DeleteTaskAsync(Guid id);

        /// <summary>
        /// Gets tasks for a specific project.
        /// </summary>
        Task<List<TrackerTask>> GetProjectTasksAsync(Guid projectId);

        /// <summary>
        /// Gets tasks for a specific goal.
        /// </summary>
        Task<List<TrackerTask>> GetGoalTasksAsync(Guid goalId);

        /// <summary>
        /// Gets tasks from a specific meeting (action items).
        /// </summary>
        Task<List<TrackerTask>> GetMeetingActionItemsAsync(Guid meetingId);

        /// <summary>
        /// Gets uncompleted tasks for a specific team member.
        /// </summary>
        Task<List<TrackerTask>> GetUncompletedTasksAsync(Guid teamMemberId);

        /// <summary>
        /// Gets tasks assigned to a specific team member.
        /// </summary>
        Task<List<TrackerTask>> GetAssignedTasksAsync(Guid teamMemberId);

        /// <summary>
        /// Gets the count of meetings where a specific task was discussed.
        /// </summary>
        Task<int> GetTaskMeetingCountAsync(Guid taskId);

        /// <summary>
        /// Gets meeting counts for multiple tasks (batch operation).
        /// </summary>
        Task<Dictionary<Guid, int>> GetTaskMeetingCountsAsync(List<Guid> taskIds);

        /// <summary>
        /// Gets subtasks of a parent task.
        /// </summary>
        Task<List<TrackerTask>> GetSubtasksAsync(Guid parentTaskId);
    }
}
