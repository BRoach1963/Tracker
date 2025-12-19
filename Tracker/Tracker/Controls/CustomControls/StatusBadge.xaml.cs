using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable status badge control that displays status with color coding.
    /// 
    /// Features:
    /// - Supports ObjectiveStatusEnum and custom status strings
    /// - Auto-color coding based on status type
    /// - Optional status dot indicator
    /// - Configurable size and appearance
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:StatusBadge Status="{Binding Status}"/&gt;
    /// &lt;controls:StatusBadge StatusText="On Track" StatusColor="#10B981"/&gt;
    /// </code>
    /// </summary>
    public partial class StatusBadge : UserControl
    {
        #region Static Color Definitions

        // Standard status colors matching design system
        private static readonly SolidColorBrush OnTrackBrush = new(Color.FromRgb(16, 185, 129));   // #10B981 - Green
        private static readonly SolidColorBrush AtRiskBrush = new(Color.FromRgb(245, 158, 11));    // #F59E0B - Amber
        private static readonly SolidColorBrush OffTrackBrush = new(Color.FromRgb(239, 68, 68));   // #EF4444 - Red
        private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(34, 197, 94)); // #22C55E - Green
        private static readonly SolidColorBrush NotStartedBrush = new(Color.FromRgb(107, 114, 128)); // #6B7280 - Gray
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(99, 102, 241));  // #6366F1 - Indigo

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The ObjectiveStatusEnum value to display.
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(ObjectiveStatusEnum?), typeof(StatusBadge),
                new PropertyMetadata(null, OnStatusChanged));

        /// <summary>
        /// Custom status text (overrides Status enum display).
        /// </summary>
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(StatusBadge),
                new PropertyMetadata(null, OnStatusChanged));

        /// <summary>
        /// Custom status color (overrides auto-coloring).
        /// </summary>
        public static readonly DependencyProperty StatusColorProperty =
            DependencyProperty.Register(nameof(StatusColor), typeof(Brush), typeof(StatusBadge),
                new PropertyMetadata(null, OnStatusChanged));

        /// <summary>
        /// Whether to show the status dot indicator.
        /// </summary>
        public static readonly DependencyProperty ShowDotProperty =
            DependencyProperty.Register(nameof(ShowDot), typeof(bool), typeof(StatusBadge),
                new PropertyMetadata(false));

        /// <summary>
        /// The corner radius of the badge.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(StatusBadge),
                new PropertyMetadata(new CornerRadius(6)));

        /// <summary>
        /// The padding inside the badge.
        /// </summary>
        public static readonly DependencyProperty BadgePaddingProperty =
            DependencyProperty.Register(nameof(BadgePadding), typeof(Thickness), typeof(StatusBadge),
                new PropertyMetadata(new Thickness(10, 4, 10, 4)));

        /// <summary>
        /// The font size of the badge text.
        /// </summary>
        public static readonly DependencyProperty BadgeFontSizeProperty =
            DependencyProperty.Register(nameof(BadgeFontSize), typeof(double), typeof(StatusBadge),
                new PropertyMetadata(10.0));

        /// <summary>
        /// The calculated status brush (read-only).
        /// </summary>
        public static readonly DependencyProperty StatusBrushProperty =
            DependencyProperty.Register(nameof(StatusBrush), typeof(Brush), typeof(StatusBadge),
                new PropertyMetadata(DefaultBrush));

        /// <summary>
        /// The calculated display text (read-only).
        /// </summary>
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(StatusBadge),
                new PropertyMetadata("Status"));

        #endregion

        #region Properties

        public ObjectiveStatusEnum? Status
        {
            get => (ObjectiveStatusEnum?)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public string? StatusText
        {
            get => (string?)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public Brush? StatusColor
        {
            get => (Brush?)GetValue(StatusColorProperty);
            set => SetValue(StatusColorProperty, value);
        }

        public bool ShowDot
        {
            get => (bool)GetValue(ShowDotProperty);
            set => SetValue(ShowDotProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Thickness BadgePadding
        {
            get => (Thickness)GetValue(BadgePaddingProperty);
            set => SetValue(BadgePaddingProperty, value);
        }

        public double BadgeFontSize
        {
            get => (double)GetValue(BadgeFontSizeProperty);
            set => SetValue(BadgeFontSizeProperty, value);
        }

        public Brush StatusBrush
        {
            get => (Brush)GetValue(StatusBrushProperty);
            private set => SetValue(StatusBrushProperty, value);
        }

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            private set => SetValue(DisplayTextProperty, value);
        }

        #endregion

        #region Constructor

        public StatusBadge()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        #endregion

        #region Private Methods

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
            {
                badge.UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            // Determine display text
            if (!string.IsNullOrEmpty(StatusText))
            {
                DisplayText = StatusText;
            }
            else if (Status.HasValue)
            {
                DisplayText = GetStatusDisplayText(Status.Value);
            }
            else
            {
                DisplayText = "Unknown";
            }

            // Determine color
            if (StatusColor != null)
            {
                StatusBrush = StatusColor;
            }
            else if (Status.HasValue)
            {
                StatusBrush = GetStatusBrush(Status.Value);
            }
            else
            {
                StatusBrush = DefaultBrush;
            }
        }

        private static string GetStatusDisplayText(ObjectiveStatusEnum status)
        {
            return status switch
            {
                ObjectiveStatusEnum.OnTrack => "On Track",
                ObjectiveStatusEnum.AtRisk => "At Risk",
                ObjectiveStatusEnum.OffTrack => "Off Track",
                _ => status.ToString()
            };
        }

        private static Brush GetStatusBrush(ObjectiveStatusEnum status)
        {
            return status switch
            {
                ObjectiveStatusEnum.OnTrack => OnTrackBrush,
                ObjectiveStatusEnum.AtRisk => AtRiskBrush,
                ObjectiveStatusEnum.OffTrack => OffTrackBrush,
                _ => DefaultBrush
            };
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Gets the standard brush for a given status.
        /// </summary>
        /// <param name="status">The status enum value.</param>
        /// <returns>The corresponding brush.</returns>
        public static Brush GetBrushForStatus(ObjectiveStatusEnum status) => GetStatusBrush(status);

        #endregion
    }
}

