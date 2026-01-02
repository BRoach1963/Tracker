using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.Services.Analytics;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// Visual indicator for trend direction (improving/stable/declining).
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:TrendIndicator Direction="Improving"/&gt;
    /// &lt;controls:TrendIndicator Direction="{Binding TrendDirection}" ShowText="True"/&gt;
    /// </code>
    /// </summary>
    public partial class TrendIndicator : UserControl
    {
        #region Static Resources

        // Arrow path data
        private const string UpArrowPath = "M 0 8 L 5 0 L 10 8 Z"; // Triangle pointing up
        private const string DownArrowPath = "M 0 0 L 5 8 L 10 0 Z"; // Triangle pointing down
        private const string StablePath = "M 0 4 L 10 4"; // Horizontal line

        // Colors
        private static readonly SolidColorBrush ImprovingBrush = new(Color.FromRgb(16, 185, 129));  // #10B981 - Green
        private static readonly SolidColorBrush DecliningBrush = new(Color.FromRgb(239, 68, 68));   // #EF4444 - Red
        private static readonly SolidColorBrush StableBrush = new(Color.FromRgb(107, 114, 128));    // #6B7280 - Gray
        private static readonly SolidColorBrush InsufficientBrush = new(Color.FromRgb(156, 163, 175)); // #9CA3AF - Light Gray

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The trend direction to display.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(nameof(Direction), typeof(TrendAnalyzer.TrendDirection), typeof(TrendIndicator),
                new PropertyMetadata(TrendAnalyzer.TrendDirection.Insufficient, OnDirectionChanged));

        /// <summary>
        /// Whether to show the text label.
        /// </summary>
        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(TrendIndicator),
                new PropertyMetadata(true));

        /// <summary>
        /// The calculated trend brush (read-only).
        /// </summary>
        public static readonly DependencyProperty TrendBrushProperty =
            DependencyProperty.Register(nameof(TrendBrush), typeof(Brush), typeof(TrendIndicator),
                new PropertyMetadata(InsufficientBrush));

        /// <summary>
        /// The calculated trend text (read-only).
        /// </summary>
        public static readonly DependencyProperty TrendTextProperty =
            DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(TrendIndicator),
                new PropertyMetadata("--"));

        /// <summary>
        /// The arrow path geometry (read-only).
        /// </summary>
        public static readonly DependencyProperty ArrowPathProperty =
            DependencyProperty.Register(nameof(ArrowPath), typeof(Geometry), typeof(TrendIndicator),
                new PropertyMetadata(Geometry.Parse(StablePath)));

        #endregion

        #region Properties

        public TrendAnalyzer.TrendDirection Direction
        {
            get => (TrendAnalyzer.TrendDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }

        public Brush TrendBrush
        {
            get => (Brush)GetValue(TrendBrushProperty);
            private set => SetValue(TrendBrushProperty, value);
        }

        public string TrendText
        {
            get => (string)GetValue(TrendTextProperty);
            private set => SetValue(TrendTextProperty, value);
        }

        public Geometry ArrowPath
        {
            get => (Geometry)GetValue(ArrowPathProperty);
            private set => SetValue(ArrowPathProperty, value);
        }

        #endregion

        #region Constructor

        public TrendIndicator()
        {
            InitializeComponent();
            UpdateVisuals();
        }

        #endregion

        #region Event Handlers

        private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrendIndicator indicator)
            {
                indicator.UpdateVisuals();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateVisuals()
        {
            switch (Direction)
            {
                case TrendAnalyzer.TrendDirection.Improving:
                    TrendBrush = ImprovingBrush;
                    TrendText = "Improving";
                    ArrowPath = Geometry.Parse(UpArrowPath);
                    break;

                case TrendAnalyzer.TrendDirection.Declining:
                    TrendBrush = DecliningBrush;
                    TrendText = "Declining";
                    ArrowPath = Geometry.Parse(DownArrowPath);
                    break;

                case TrendAnalyzer.TrendDirection.Stable:
                    TrendBrush = StableBrush;
                    TrendText = "Stable";
                    ArrowPath = Geometry.Parse(StablePath);
                    break;

                default:
                    TrendBrush = InsufficientBrush;
                    TrendText = "--";
                    ArrowPath = Geometry.Parse(StablePath);
                    break;
            }
        }

        #endregion
    }
}
