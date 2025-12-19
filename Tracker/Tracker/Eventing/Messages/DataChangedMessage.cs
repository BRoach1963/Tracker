using CommunityToolkit.Mvvm.Messaging.Messages;
using Tracker.Common.Enums;

namespace Tracker.Eventing.Messages
{
    /// <summary>
    /// Message sent when data changes and views should refresh.
    /// Uses CommunityToolkit.Mvvm WeakReferenceMessenger for proper memory management.
    /// </summary>
    public class DataChangedMessage : ValueChangedMessage<DataChangeInfo>
    {
        public DataChangedMessage(DataChangeInfo value) : base(value)
        {
        }

        /// <summary>
        /// Creates a message indicating all data should be refreshed.
        /// </summary>
        public static DataChangedMessage All() => 
            new(new DataChangeInfo(DataChangeType.All));

        /// <summary>
        /// Creates a message for a specific data type change.
        /// </summary>
        public static DataChangedMessage ForType(DataChangeType type) => 
            new(new DataChangeInfo(type));

        /// <summary>
        /// Creates a message for multiple data type changes.
        /// </summary>
        public static DataChangedMessage ForTypes(params DataChangeType[] types) => 
            new(new DataChangeInfo(types));
    }

    /// <summary>
    /// Information about what data changed.
    /// </summary>
    public class DataChangeInfo
    {
        public DataChangeType[] ChangedTypes { get; }
        public bool RefreshAll => ChangedTypes.Contains(DataChangeType.All);

        public DataChangeInfo(DataChangeType type)
        {
            ChangedTypes = new[] { type };
        }

        public DataChangeInfo(DataChangeType[] types)
        {
            ChangedTypes = types;
        }

        public bool Includes(DataChangeType type) => 
            RefreshAll || ChangedTypes.Contains(type);
    }

    /// <summary>
    /// Types of data that can change.
    /// </summary>
    public enum DataChangeType
    {
        All,
        TeamMembers,
        OneOnOnes,
        Tasks,
        Projects,
        OKRs,
        KPIs,
        Goals,
        Feedback,
        QuickNotes,
        Settings,
        UserProfile
    }
}

