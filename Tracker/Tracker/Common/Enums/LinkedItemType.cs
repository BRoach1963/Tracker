using System.ComponentModel;

namespace Tracker.Common.Enums
{
    /// <summary>
    /// Specifies the type of item that can be linked to a 1:1 meeting.
    /// </summary>
    public enum LinkedItemType
    {
        /// <summary>
        /// An individual task, action item, or follow-up item.
        /// </summary>
        [Description("Task")]
        Task = 0,

        /// <summary>
        /// An Objective and Key Result.
        /// </summary>
        [Description("OKR")]
        OKR = 1,

        /// <summary>
        /// A Key Performance Indicator.
        /// </summary>
        [Description("KPI")]
        KPI = 2,

        /// <summary>
        /// A project.
        /// </summary>
        [Description("Project")]
        Project = 3,

        /// <summary>
        /// A concern raised in a previous meeting.
        /// </summary>
        [Description("Concern")]
        Concern = 4,

        /// <summary>
        /// A discussion point from a previous meeting.
        /// </summary>
        [Description("Discussion Point")]
        DiscussionPoint = 5
    }
}

