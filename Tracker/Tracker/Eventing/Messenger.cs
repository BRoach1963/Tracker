namespace Tracker.Eventing
{
    public class Messenger
    {
        private static readonly Dictionary<Type, List<Delegate>> Subscriptions = new Dictionary<Type, List<Delegate>>();
        private static readonly object LockObj = new object();

        public static void Subscribe<TMessage>(Action<TMessage> action)
        {
            var messageType = typeof(TMessage);

            lock (LockObj)
            {
                if (!Subscriptions.ContainsKey(messageType))
                {
                    Subscriptions[messageType] = new List<Delegate>();
                }

                Subscriptions[messageType].Add(action);
            }
        }

        public static void Unsubscribe<TMessage>(Action<TMessage> action)
        {
            var messageType = typeof(TMessage);

            lock (LockObj)
            {
                if (Subscriptions.TryGetValue(messageType, out var subscription))
                {
                    subscription.Remove(action);
                }
            }
        }

        public static void Publish<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);
            List<Delegate> handlersToInvoke;

            lock (LockObj)
            {
                if (!Subscriptions.ContainsKey(messageType)) return;

                // Make a copy of the delegate list to invoke outside of the lock to avoid potential deadlocks
                // if one of the handlers tries to modify the subscriptions
                handlersToInvoke = new List<Delegate>(Subscriptions[messageType]);
            }

            foreach (var sub in handlersToInvoke)
            {
                (sub as Action<TMessage>)?.Invoke(message);
            }
        }
    }
}
