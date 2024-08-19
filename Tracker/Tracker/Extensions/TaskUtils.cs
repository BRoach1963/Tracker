using Tracker.Logging;

namespace Tracker.Extensions
{
    public static class TaskUtils
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(TaskUtils));

        /// <summary>
        /// Extension method to gracefully handle any exceptions that occur when executing a task
        /// </summary>
        /// <param name="task"></param>
        public static void LogExceptions(this Task task)
        {
            task.ContinueWith(t =>
                {
                    var aggException = t.Exception.Flatten();

                    foreach (var exception in aggException.InnerExceptions)
                    {
                        _logger.Exception(exception, "Task encountered exception: ");
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
