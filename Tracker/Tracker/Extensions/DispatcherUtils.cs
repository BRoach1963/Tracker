using System.Windows.Threading;

namespace Tracker.Extensions
{
    public static class DispatcherUtils
    {
        public static TResult BackgroundInvoke<TResult>(this Dispatcher dispatcher, Func<TResult> action)
        {
            if (!dispatcher.CheckAccess())
            {
                return dispatcher.Invoke(action, DispatcherPriority.Normal);
            }
            else
            {
                // already on the UI thread
                return action.Invoke();
            }
        }

        public static void BackgroundInvoke(this Dispatcher dispatcher, Action action)
        {
            // if this call is not already being made on the UI thread, dispatch
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(action, DispatcherPriority.Normal);
            }
            else
            {
                // already on the UI thread
                action.Invoke();
            }
        }

        public static void DispatchIfNecessary(this Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Render)
        {
            // if this call is not already being made on the UI thread, dispatch
            if (!dispatcher.CheckAccess())
            {
                dispatcher.InvokeAsync(action, priority).Task.LogExceptions(aggException: null);
            }
            else
            {
                // already on the UI thread
                action.Invoke();
            }
        }

        public static async Task<TResult> DispatchIfNecessaryAsync<TResult>(this Dispatcher dispatcher, Func<TResult> callback, DispatcherPriority priority = DispatcherPriority.Render)
        {
            // if this call is not already being made on the UI thread, dispatch
            if (!dispatcher.CheckAccess())
            {
                return await dispatcher.InvokeAsync(callback, priority);
            }
            else
            {
                // already on the UI thread
                return callback.Invoke();
            }
        }
    }
}
