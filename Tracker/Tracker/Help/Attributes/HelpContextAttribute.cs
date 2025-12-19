namespace Tracker.Help.Attributes
{
    /// <summary>
    /// Marks a control, window, or property with a help topic identifier.
    /// Used by the help system to provide context-sensitive help when F1 is pressed.
    /// </summary>
    /// <example>
    /// // On a UserControl
    /// [HelpContext("features/team-members")]
    /// public partial class TeamMembersControl : UserControl { }
    /// 
    /// // On a dialog with specific section
    /// [HelpContext("dialogs/add-one-on-one", "agenda-tab")]
    /// public partial class AddOneOnOneDialog : BaseWindow { }
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HelpContextAttribute : Attribute
    {
        /// <summary>
        /// The help topic identifier (maps to a markdown file path).
        /// Example: "features/team-members" maps to Resources/Help/features/team-members.md
        /// </summary>
        public string TopicId { get; }

        /// <summary>
        /// Optional section anchor within the topic (for deep linking to specific content).
        /// Example: "adding-a-member" would scroll to ## Adding a Member
        /// </summary>
        public string? Section { get; }

        /// <summary>
        /// Optional display title override. If not set, uses the topic's H1 heading.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Creates a new HelpContext attribute.
        /// </summary>
        /// <param name="topicId">The topic identifier (relative path without .md extension)</param>
        /// <param name="section">Optional section anchor within the topic</param>
        public HelpContextAttribute(string topicId, string? section = null)
        {
            TopicId = topicId ?? throw new ArgumentNullException(nameof(topicId));
            Section = section;
        }
    }

    /// <summary>
    /// Marks a field or property with field-level help text.
    /// Shows as an enhanced tooltip or inline help.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class HelpFieldAttribute : Attribute
    {
        /// <summary>
        /// Brief description of the field (shown in tooltip).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Optional link to detailed help topic.
        /// </summary>
        public string? TopicId { get; set; }

        /// <summary>
        /// Optional example value.
        /// </summary>
        public string? Example { get; set; }

        public HelpFieldAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }
}

