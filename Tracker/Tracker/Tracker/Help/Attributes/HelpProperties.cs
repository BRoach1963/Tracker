using System.Windows;

namespace Tracker.Help.Attributes
{
    /// <summary>
    /// Attached properties for adding context-sensitive help to any XAML element.
    /// These take precedence over HelpContextAttribute when resolving help context.
    /// </summary>
    /// <example>
    /// &lt;TabItem Header="Goals" 
    ///          help:HelpProperties.TopicId="dialogs/add-team-member"
    ///          help:HelpProperties.Section="goals-tab" /&gt;
    /// </example>
    public static class HelpProperties
    {
        #region TopicId Attached Property

        /// <summary>
        /// Gets the help topic ID for an element.
        /// </summary>
        public static string GetTopicId(DependencyObject obj)
        {
            return (string)obj.GetValue(TopicIdProperty);
        }

        /// <summary>
        /// Sets the help topic ID for an element.
        /// </summary>
        public static void SetTopicId(DependencyObject obj, string value)
        {
            obj.SetValue(TopicIdProperty, value);
        }

        /// <summary>
        /// Identifies the TopicId attached property.
        /// </summary>
        public static readonly DependencyProperty TopicIdProperty =
            DependencyProperty.RegisterAttached(
                "TopicId",
                typeof(string),
                typeof(HelpProperties),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        #endregion

        #region Section Attached Property

        /// <summary>
        /// Gets the help section anchor for an element.
        /// </summary>
        public static string GetSection(DependencyObject obj)
        {
            return (string)obj.GetValue(SectionProperty);
        }

        /// <summary>
        /// Sets the help section anchor for an element.
        /// </summary>
        public static void SetSection(DependencyObject obj, string value)
        {
            obj.SetValue(SectionProperty, value);
        }

        /// <summary>
        /// Identifies the Section attached property.
        /// </summary>
        public static readonly DependencyProperty SectionProperty =
            DependencyProperty.RegisterAttached(
                "Section",
                typeof(string),
                typeof(HelpProperties),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        #endregion

        #region Title Attached Property

        /// <summary>
        /// Gets the help title override for an element.
        /// </summary>
        public static string GetTitle(DependencyObject obj)
        {
            return (string)obj.GetValue(TitleProperty);
        }

        /// <summary>
        /// Sets the help title override for an element.
        /// </summary>
        public static void SetTitle(DependencyObject obj, string value)
        {
            obj.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the Title attached property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.RegisterAttached(
                "Title",
                typeof(string),
                typeof(HelpProperties),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        #endregion

        #region FieldHelp Attached Property

        /// <summary>
        /// Gets the field-level help text for an element (shown as enhanced tooltip).
        /// </summary>
        public static string GetFieldHelp(DependencyObject obj)
        {
            return (string)obj.GetValue(FieldHelpProperty);
        }

        /// <summary>
        /// Sets the field-level help text for an element.
        /// </summary>
        public static void SetFieldHelp(DependencyObject obj, string value)
        {
            obj.SetValue(FieldHelpProperty, value);
        }

        /// <summary>
        /// Identifies the FieldHelp attached property.
        /// </summary>
        public static readonly DependencyProperty FieldHelpProperty =
            DependencyProperty.RegisterAttached(
                "FieldHelp",
                typeof(string),
                typeof(HelpProperties),
                new PropertyMetadata(null, OnFieldHelpChanged));

        private static void OnFieldHelpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is string helpText && !string.IsNullOrEmpty(helpText))
            {
                // Set as tooltip if not already set
                if (element.ToolTip == null)
                {
                    element.ToolTip = helpText;
                }
            }
        }

        #endregion
    }
}

