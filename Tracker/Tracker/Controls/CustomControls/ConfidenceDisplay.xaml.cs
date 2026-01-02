using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.Services.Analytics;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// Visual display for prediction confidence levels using signal bars.
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:ConfidenceDisplay Level="High"/&gt;
    /// &lt;controls:ConfidenceDisplay Level="{Binding ConfidenceLevel}" ShowText="True"/&gt;
    /// </code>
    /// </summary>
    public partial class ConfidenceDisplay : UserControl
    {
        #region Static Resources

        private static readonly SolidColorBrush HighBrush = new(Color.FromRgb(16, 185, 129));      // #10B981 - Green
        private static readonly SolidColorBrush MediumBrush = new(Color.FromRgb(245, 158, 11));    // #F59E0B - Amber
        private static readonly SolidColorBrush LowBrush = new(Color.FromRgb(239, 68, 68));        // #EF4444 - Red
        private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(209, 213, 219)); // #D1D5DB - Light Gray
        private static readonly SolidColorBrush TextGrayBrush = new(Color.FromRgb(107, 114, 128)); // #6B7280 - Gray

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The confidence level to display.
        /// </summary>
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(nameof(Level), typeof(DataSufficiencyChecker.ConfidenceLevel), typeof(ConfidenceDisplay),
                new PropertyMetadata(DataSufficiencyChecker.ConfidenceLevel.Insufficient, OnLevelChanged));

        /// <summary>
        /// The confidence score (0-100) for tooltip display.
        /// </summary>
        public static readonly DependencyProperty ScoreProperty =
            DependencyProperty.Register(nameof(Score), typeof(double), typeof(ConfidenceDisplay),
                new PropertyMetadata(0.0, OnScoreChanged));

        /// <summary>
        /// Whether to show the text label.
        /// </summary>
        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(ConfidenceDisplay),
                new PropertyMetadata(true));

        // Bar brushes
        public static readonly DependencyProperty Bar1BrushProperty =
            DependencyProperty.Register(nameof(Bar1Brush), typeof(Brush), typeof(ConfidenceDisplay),
                new PropertyMetadata(InactiveBrush));

        public static readonly DependencyProperty Bar2BrushProperty =
            DependencyProperty.Register(nameof(Bar2Brush), typeof(Brush), typeof(ConfidenceDisplay),
                new PropertyMetadata(InactiveBrush));

        public static readonly DependencyProperty Bar3BrushProperty =
            DependencyProperty.Register(nameof(Bar3Brush), typeof(Brush), typeof(ConfidenceDisplay),
                new PropertyMetadata(InactiveBrush));

        public static readonly DependencyProperty Bar4BrushProperty =
            DependencyProperty.Register(nameof(Bar4Brush), typeof(Brush), typeof(ConfidenceDisplay),
                new PropertyMetadata(InactiveBrush));

        public static readonly DependencyProperty ConfidenceTextProperty =
            DependencyProperty.Register(nameof(ConfidenceText), typeof(string), typeof(ConfidenceDisplay),
                new PropertyMetadata("--"));

        public static readonly DependencyProperty TextBrushProperty =
            DependencyProperty.Register(nameof(TextBrush), typeof(Brush), typeof(ConfidenceDisplay),
                new PropertyMetadata(TextGrayBrush));

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(ConfidenceDisplay),
                new PropertyMetadata("Prediction confidence"));

        #endregion

        #region Properties

        public DataSufficiencyChecker.ConfidenceLevel Level
        {
            get => (DataSufficiencyChecker.ConfidenceLevel)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        public double Score
        {
            get => (double)GetValue(ScoreProperty);
            set => SetValue(ScoreProperty, value);
        }

        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }

        public Brush Bar1Brush
        {
            get => (Brush)GetValue(Bar1BrushProperty);
            private set => SetValue(Bar1BrushProperty, value);
        }

        public Brush Bar2Brush
        {
            get => (Brush)GetValue(Bar2BrushProperty);
            private set => SetValue(Bar2BrushProperty, value);
        }

        public Brush Bar3Brush
        {
            get => (Brush)GetValue(Bar3BrushProperty);
            private set => SetValue(Bar3BrushProperty, value);
        }

        public Brush Bar4Brush
        {
            get => (Brush)GetValue(Bar4BrushProperty);
            private set => SetValue(Bar4BrushProperty, value);
        }

        public string ConfidenceText
        {
            get => (string)GetValue(ConfidenceTextProperty);
            private set => SetValue(ConfidenceTextProperty, value);
        }

        public Brush TextBrush
        {
            get => (Brush)GetValue(TextBrushProperty);
            private set => SetValue(TextBrushProperty, value);
        }

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            private set => SetValue(TooltipTextProperty, value);
        }

        #endregion

        #region Constructor

        public ConfidenceDisplay()
        {
            InitializeComponent();
            UpdateVisuals();
        }

        #endregion

        #region Event Handlers

        private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConfidenceDisplay display)
            {
                display.UpdateVisuals();
            }
        }

        private static void OnScoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConfidenceDisplay display)
            {
                display.UpdateTooltip();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateVisuals()
        {
            var activeBrush = GetActiveBrush();

            switch (Level)
            {
                case DataSufficiencyChecker.ConfidenceLevel.High:
                    Bar1Brush = activeBrush;
                    Bar2Brush = activeBrush;
                    Bar3Brush = activeBrush;
                    Bar4Brush = activeBrush;
                    ConfidenceText = "High";
                    TextBrush = HighBrush;
                    break;

                case DataSufficiencyChecker.ConfidenceLevel.Medium:
                    Bar1Brush = activeBrush;
                    Bar2Brush = activeBrush;
                    Bar3Brush = activeBrush;
                    Bar4Brush = InactiveBrush;
                    ConfidenceText = "Medium";
                    TextBrush = MediumBrush;
                    break;

                case DataSufficiencyChecker.ConfidenceLevel.Low:
                    Bar1Brush = activeBrush;
                    Bar2Brush = activeBrush;
                    Bar3Brush = InactiveBrush;
                    Bar4Brush = InactiveBrush;
                    ConfidenceText = "Low";
                    TextBrush = LowBrush;
                    break;

                case DataSufficiencyChecker.ConfidenceLevel.VeryLow:
                    Bar1Brush = activeBrush;
                    Bar2Brush = InactiveBrush;
                    Bar3Brush = InactiveBrush;
                    Bar4Brush = InactiveBrush;
                    ConfidenceText = "Very Low";
                    TextBrush = LowBrush;
                    break;

                default: // Insufficient
                    Bar1Brush = InactiveBrush;
                    Bar2Brush = InactiveBrush;
                    Bar3Brush = InactiveBrush;
                    Bar4Brush = InactiveBrush;
                    ConfidenceText = "No Data";
                    TextBrush = TextGrayBrush;
                    break;
            }

            UpdateTooltip();
        }

        private Brush GetActiveBrush()
        {
            return Level switch
            {
                DataSufficiencyChecker.ConfidenceLevel.High => HighBrush,
                DataSufficiencyChecker.ConfidenceLevel.Medium => MediumBrush,
                DataSufficiencyChecker.ConfidenceLevel.Low => LowBrush,
                DataSufficiencyChecker.ConfidenceLevel.VeryLow => LowBrush,
                _ => InactiveBrush
            };
        }

        private void UpdateTooltip()
        {
            if (Score > 0)
            {
                TooltipText = $"Prediction confidence: {Score:F0}%\n{DataSufficiencyChecker.GetConfidenceDisplayText(Level)}";
            }
            else
            {
                TooltipText = DataSufficiencyChecker.GetConfidenceDisplayText(Level);
            }
        }

        #endregion
    }
}
