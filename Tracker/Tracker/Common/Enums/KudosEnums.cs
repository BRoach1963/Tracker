namespace Tracker.Common.Enums
{
    /// <summary>
    /// Categories for kudos/recognition types.
    /// </summary>
    public enum KudosCategory
    {
        /// <summary>Collaboration and teamwork.</summary>
        TeamWork,
        
        /// <summary>Creative problem solving or new ideas.</summary>
        Innovation,
        
        /// <summary>Leading by example or mentoring others.</summary>
        Leadership,
        
        /// <summary>Going above and beyond for customers.</summary>
        CustomerFocus,
        
        /// <summary>Exceeding expectations.</summary>
        GoingAboveBeyond,
        
        /// <summary>Effective problem resolution.</summary>
        ProblemSolving,
        
        /// <summary>Personal or professional growth.</summary>
        LearningGrowth,
        
        /// <summary>Consistent and dependable performance.</summary>
        Reliability,
        
        /// <summary>Clear and effective communication.</summary>
        Communication,
        
        /// <summary>Other recognition type.</summary>
        Other
    }

    /// <summary>
    /// Channels through which kudos can be delivered.
    /// </summary>
    public enum DeliveryChannel
    {
        /// <summary>Microsoft Teams via incoming webhook.</summary>
        MicrosoftTeams,
        
        /// <summary>Slack via Bot API.</summary>
        Slack,
        
        /// <summary>Email delivery.</summary>
        Email,
        
        /// <summary>Internal only - logged in Tracker but not delivered externally.</summary>
        InternalOnly
    }

    /// <summary>
    /// Status of kudos delivery.
    /// </summary>
    public enum DeliveryStatus
    {
        /// <summary>Kudos created but not yet sent.</summary>
        Draft,
        
        /// <summary>Kudos scheduled for future delivery.</summary>
        Scheduled,
        
        /// <summary>Kudos is currently being sent.</summary>
        Sending,
        
        /// <summary>Kudos was successfully delivered.</summary>
        Delivered,
        
        /// <summary>Kudos delivery failed.</summary>
        Failed
    }
}
