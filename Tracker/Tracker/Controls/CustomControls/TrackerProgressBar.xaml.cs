using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable progress bar control with customizable appearance.
    /// 
    /// Features:
    /// - Configurable value/maximum for percentage calculation
    /// - Optional percentage label display
    /// - Customizable colors via brushes (or auto-uses theme colors)
    /// - Adjustable height and corner radius
    /// - Smooth animations (optional)
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:TrackerProgressBar Value="75" Maximum="100" ShowPercentage="True"/&gt;
    /// &lt;controls:TrackerProgressBar Value="{Binding Progress}" FillBrush="{DynamicResource AccentBrush}"/&gt;
    /// </code>
    /// </summary>
    public partial class TrackerProgressBar : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// The current value of the progress bar.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(TrackerProgressBar),
                new PropertyMetadata(0m, OnValueChanged));

        /// <summary>
        /// The maximum value of the progress bar (default 100).
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(decimal), typeof(TrackerProgressBar),
                new PropertyMetadata(100m, OnValueChanged));

        /// <summary>
        /// Whether to show the percentage label.
        /// </summary>
        public static readonly DependencyProperty ShowPercentageProperty =
            DependencyProperty.Register(nameof(ShowPercentage), typeof(bool), typeof(TrackerProgressBar),
                new PropertyMetadata(true));

        /// <summary>
        /// The brush used for the track (background).
        /// </summary>
        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(TrackerProgressBar),
                new PropertyMetadata(null));

        /// <summary>
        /// The brush used for the fill (progress).
        /// </summary>
        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(TrackerProgressBar),
                new PropertyMetadata(null));

        /// <summary>
        /// The brush used for the percentage label.
        /// </summary>
        public static readonly DependencyProperty LabelBrushProperty =
            DependencyProperty.Register(nameof(LabelBrush), typeof(Brush), typeof(TrackerProgressBar),
                new PropertyMetadata(null));

        /// <summary>
        /// The corner radius for the progress bar.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(TrackerProgressBar),
                new PropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// The calculated display percentage (read-only).
        /// </summary>
        public static readonly DependencyProperty DisplayPercentageProperty =
            DependencyProperty.Register(nameof(DisplayPercentage), typeof(int), typeof(TrackerProgressBar),
                new PropertyMetadata(0));

        #endregion

        #region Properties

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public decimal Maximum
        {
            get => (decimal)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public bool ShowPercentage
        {
            get => (bool)GetValue(ShowPercentageProperty);
            set => SetValue(ShowPercentageProperty, value);
        }

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty) ?? (Brush)FindResource("BackgroundBrush");
            set => SetValue(TrackBrushProperty, value);
        }

        public Brush FillBrush
        {
            get => (Brush)GetValue(FillBrushProperty) ?? (Brush)FindResource("AccentBrush");
            set => SetValue(FillBrushProperty, value);
        }

        public Brush LabelBrush
        {
            get => (Brush)GetValue(LabelBrushProperty) ?? (Brush)FindResource("ForegroundBrush");
            set => SetValue(LabelBrushProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public int DisplayPercentage
        {
            get => (int)GetValue(DisplayPercentageProperty);
            private set => SetValue(DisplayPercentageProperty, value);
        }

        #endregion

        #region Constructor

        public TrackerProgressBar()
        {
            InitializeComponent();
            UpdateDisplayPercentage();
        }

        #endregion

        #region Private Methods

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackerProgressBar progressBar)
            {
                progressBar.UpdateDisplayPercentage();
            }
        }

        private void UpdateDisplayPercentage()
        {
            if (Maximum == 0)
            {
                DisplayPercentage = 0;
                return;
            }

            var percentage = (Value / Maximum) * 100;
            DisplayPercentage = (int)Math.Round(Math.Min(100, Math.Max(0, percentage)));
        }

        #endregion
    }
}

