using CommunityToolkit.Mvvm.Messaging;
using Tracker.Eventing.Messages;
using Tracker.Logging;

namespace Tracker.Eventing
{
    /// <summary>
    /// Static helper for sending data change messages.
    /// Wraps CommunityToolkit.Mvvm WeakReferenceMessenger for easy use across the app.
    /// </summary>
    public static class DataMessenger
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger("DataMessenger");
        
        /// <summary>
        /// Sends a message to refresh all data.
        /// </summary>
        public static void SendRefreshAll()
        {
            _logger.Info("SendRefreshAll called - broadcasting to all registered recipients");
            WeakReferenceMessenger.Default.Send(DataChangedMessage.All());
            _logger.Debug("SendRefreshAll message sent");
        }

        /// <summary>
        /// Sends a message to refresh a specific data type.
        /// </summary>
        public static void SendRefresh(DataChangeType type)
        {
            WeakReferenceMessenger.Default.Send(DataChangedMessage.ForType(type));
        }

        /// <summary>
        /// Sends a message to refresh multiple data types.
        /// </summary>
        public static void SendRefresh(params DataChangeType[] types)
        {
            WeakReferenceMessenger.Default.Send(DataChangedMessage.ForTypes(types));
        }

        /// <summary>
        /// Registers a recipient to receive data change messages.
        /// Call this in your ViewModel constructor.
        /// </summary>
        public static void Register<TRecipient>(TRecipient recipient, Action<DataChangeInfo> handler)
            where TRecipient : class
        {
            _logger.Debug("Registering {0} for DataChangedMessage", typeof(TRecipient).Name);
            WeakReferenceMessenger.Default.Register<TRecipient, DataChangedMessage>(
                recipient,
                (r, m) => handler(m.Value));
        }

        /// <summary>
        /// Unregisters a recipient from receiving messages.
        /// Call this when the ViewModel is disposed.
        /// </summary>
        public static void Unregister<TRecipient>(TRecipient recipient)
            where TRecipient : class
        {
            WeakReferenceMessenger.Default.Unregister<DataChangedMessage>(recipient);
        }

        /// <summary>
        /// Unregisters all messages for a recipient.
        /// </summary>
        public static void UnregisterAll(object recipient)
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }
}

