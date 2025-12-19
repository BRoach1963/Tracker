using System.Windows;
using System.Windows.Controls;

namespace Tracker.Controls
{
    /// <summary>
    /// Modern tooltip control with support for title, description, and keyboard shortcut hints.
    /// Honors application theming.
    /// </summary>
    public partial class TrackerToolTip : UserControl
    {
        public TrackerToolTip()
        {
            InitializeComponent();
        }

        #region ToolTipText Property
        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(nameof(ToolTipText), typeof(string),
                typeof(TrackerToolTip), new PropertyMetadata(string.Empty));

        /// <summary>
        /// The main tooltip text/description.
        /// </summary>
        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }
        #endregion

        #region Title Property
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string),
                typeof(TrackerToolTip), new PropertyMetadata(string.Empty, OnTitleChanged));

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackerToolTip tooltip)
            {
                tooltip.HasTitle = !string.IsNullOrWhiteSpace(e.NewValue as string);
            }
        }

        /// <summary>
        /// Optional title displayed above the main text.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region HasTitle Property (Read-only)
        private static readonly DependencyPropertyKey HasTitlePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasTitle), typeof(bool),
                typeof(TrackerToolTip), new PropertyMetadata(false));

        public static readonly DependencyProperty HasTitleProperty = HasTitlePropertyKey.DependencyProperty;

        /// <summary>
        /// Whether a title is set (for visibility binding).
        /// </summary>
        public bool HasTitle
        {
            get => (bool)GetValue(HasTitleProperty);
            private set => SetValue(HasTitlePropertyKey, value);
        }
        #endregion

        #region Shortcut Property
        public static readonly DependencyProperty ShortcutProperty =
            DependencyProperty.Register(nameof(Shortcut), typeof(string),
                typeof(TrackerToolTip), new PropertyMetadata(string.Empty, OnShortcutChanged));

        private static void OnShortcutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackerToolTip tooltip)
            {
                tooltip.HasShortcut = !string.IsNullOrWhiteSpace(e.NewValue as string);
            }
        }

        /// <summary>
        /// Optional keyboard shortcut hint (e.g., "Ctrl+S").
        /// </summary>
        public string Shortcut
        {
            get => (string)GetValue(ShortcutProperty);
            set => SetValue(ShortcutProperty, value);
        }
        #endregion

        #region HasShortcut Property (Read-only)
        private static readonly DependencyPropertyKey HasShortcutPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasShortcut), typeof(bool),
                typeof(TrackerToolTip), new PropertyMetadata(false));

        public static readonly DependencyProperty HasShortcutProperty = HasShortcutPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether a shortcut is set (for visibility binding).
        /// </summary>
        public bool HasShortcut
        {
            get => (bool)GetValue(HasShortcutProperty);
            private set => SetValue(HasShortcutPropertyKey, value);
        }
        #endregion
    }
}
