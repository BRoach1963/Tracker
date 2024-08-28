using Tracker.Logging;

namespace Tracker.Extensions
{
    public static class TaskUtils
    {
        private static readonly ILogger Logger = LoggingManager.GetComponentLogger(nameof(TaskUtils));

        /// <summary>
        /// Extension method to gracefully handle any exceptions that occur when executing a task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="aggException"></param>
        public static void LogExceptions(this Task task, AggregateException? aggException)
        {
            _ = task.ContinueWith(static t =>
                {
                    var aggException = t.Exception?.Flatten();

                    if (aggException?.InnerExceptions == null) return;
                    {
                        foreach (var exception in aggException.InnerExceptions)
                        {
                            Logger.Exception(exception, "Task encountered exception: ");
                        }
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
