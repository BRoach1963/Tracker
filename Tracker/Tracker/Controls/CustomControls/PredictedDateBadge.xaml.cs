using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// Badge showing predicted completion date with comparison to target.
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:PredictedDateBadge 
    ///     PredictedDate="{Binding PredictedCompletion}"
    ///     TargetDate="{Binding DueDate}"/&gt;
    /// </code>
    /// </summary>
    public partial class PredictedDateBadge : UserControl
    {
        #region Static Resources

        private static readonly SolidColorBrush OnTimeBrush = new(Color.FromRgb(16, 185, 129));    // #10B981 - Green
        private static readonly SolidColorBrush EarlyBrush = new(Color.FromRgb(34, 197, 94));      // #22C55E - Bright Green
        private static readonly SolidColorBrush SlightlyLateBrush = new(Color.FromRgb(245, 158, 11)); // #F59E0B - Amber
        private static readonly SolidColorBrush LateBrush = new(Color.FromRgb(239, 68, 68));       // #EF4444 - Red
        private static readonly SolidColorBrush UnknownBrush = new(Color.FromRgb(107, 114, 128)); // #6B7280 - Gray

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The predicted completion date.
        /// </summary>
        public static readonly DependencyProperty PredictedDateProperty =
            DependencyProperty.Register(nameof(PredictedDate), typeof(DateTime?), typeof(PredictedDateBadge),
                new PropertyMetadata(null, OnDatesChanged));

        /// <summary>
        /// The target/due date for comparison.
        /// </summary>
        public static readonly DependencyProperty TargetDateProperty =
            DependencyProperty.Register(nameof(TargetDate), typeof(DateTime?), typeof(PredictedDateBadge),
                new PropertyMetadata(null, OnDatesChanged));

        /// <summary>
        /// Days difference from target (positive = early, negative = late).
        /// </summary>
        public static readonly DependencyProperty DaysFromTargetProperty =
            DependencyProperty.Register(nameof(DaysFromTarget), typeof(int?), typeof(PredictedDateBadge),
                new PropertyMetadata(null, OnDatesChanged));

        // Display properties
        public static readonly DependencyProperty BadgeBrushProperty =
            DependencyProperty.Register(nameof(BadgeBrush), typeof(Brush), typeof(PredictedDateBadge),
                new PropertyMetadata(UnknownBrush));

        public static readonly DependencyProperty PredictionLabelProperty =
            DependencyProperty.Register(nameof(PredictionLabel), typeof(string), typeof(PredictedDateBadge),
                new PropertyMetadata("Predicted"));

        public static readonly DependencyProperty DateTextProperty =
            DependencyProperty.Register(nameof(DateText), typeof(string), typeof(PredictedDateBadge),
                new PropertyMetadata("--"));

        public static readonly DependencyProperty DeltaTextProperty =
            DependencyProperty.Register(nameof(DeltaText), typeof(string), typeof(PredictedDateBadge),
                new PropertyMetadata(""));

        public static readonly DependencyProperty ShowDeltaProperty =
            DependencyProperty.Register(nameof(ShowDelta), typeof(bool), typeof(PredictedDateBadge),
                new PropertyMetadata(false));

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(PredictedDateBadge),
                new PropertyMetadata("Predicted completion date"));

        #endregion

        #region Properties

        public DateTime? PredictedDate
        {
            get => (DateTime?)GetValue(PredictedDateProperty);
            set => SetValue(PredictedDateProperty, value);
        }

        public DateTime? TargetDate
        {
            get => (DateTime?)GetValue(TargetDateProperty);
            set => SetValue(TargetDateProperty, value);
        }

        public int? DaysFromTarget
        {
            get => (int?)GetValue(DaysFromTargetProperty);
            set => SetValue(DaysFromTargetProperty, value);
        }

        public Brush BadgeBrush
        {
            get => (Brush)GetValue(BadgeBrushProperty);
            private set => SetValue(BadgeBrushProperty, value);
        }

        public string PredictionLabel
        {
            get => (string)GetValue(PredictionLabelProperty);
            private set => SetValue(PredictionLabelProperty, value);
        }

        public string DateText
        {
            get => (string)GetValue(DateTextProperty);
            private set => SetValue(DateTextProperty, value);
        }

        public string DeltaText
        {
            get => (string)GetValue(DeltaTextProperty);
            private set => SetValue(DeltaTextProperty, value);
        }

        public bool ShowDelta
        {
            get => (bool)GetValue(ShowDeltaProperty);
            private set => SetValue(ShowDeltaProperty, value);
        }

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            private set => SetValue(TooltipTextProperty, value);
        }

        #endregion

        #region Constructor

        public PredictedDateBadge()
        {
            InitializeComponent();
            UpdateVisuals();
        }

        #endregion

        #region Event Handlers

        private static void OnDatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PredictedDateBadge badge)
            {
                badge.UpdateVisuals();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateVisuals()
        {
            if (!PredictedDate.HasValue)
            {
                BadgeBrush = UnknownBrush;
                PredictionLabel = "Predicted";
                DateText = "Unknown";
                ShowDelta = false;
                TooltipText = "Unable to predict completion date";
                return;
            }

            // Format the date
            var predicted = PredictedDate.Value;
            DateText = FormatDate(predicted);
            PredictionLabel = "Predicted";

            // Calculate days difference
            int? delta = DaysFromTarget;
            if (!delta.HasValue && TargetDate.HasValue)
            {
                delta = (TargetDate.Value - predicted).Days;
            }

            // Set color and delta text based on comparison
            if (delta.HasValue)
            {
                if (delta.Value > 7) // More than a week early
                {
                    BadgeBrush = EarlyBrush;
                    DeltaText = $"{delta.Value}d early";
                    ShowDelta = true;
                    TooltipText = $"Predicted to complete {delta.Value} days ahead of schedule";
                }
                else if (delta.Value > 0) // Slightly early
                {
                    BadgeBrush = OnTimeBrush;
                    DeltaText = $"{delta.Value}d early";
                    ShowDelta = true;
                    TooltipText = $"Predicted to complete {delta.Value} days ahead of schedule";
                }
                else if (delta.Value == 0) // On time
                {
                    BadgeBrush = OnTimeBrush;
                    DeltaText = "On time";
                    ShowDelta = true;
                    TooltipText = "Predicted to complete on target date";
                }
                else if (delta.Value > -7) // Slightly late (within a week)
                {
                    BadgeBrush = SlightlyLateBrush;
                    DeltaText = $"{Math.Abs(delta.Value)}d late";
                    ShowDelta = true;
                    TooltipText = $"Predicted to complete {Math.Abs(delta.Value)} days behind schedule";
                }
                else // More than a week late
                {
                    BadgeBrush = LateBrush;
                    DeltaText = $"{Math.Abs(delta.Value)}d late";
                    ShowDelta = true;
                    TooltipText = $"Predicted to complete {Math.Abs(delta.Value)} days behind schedule";
                }
            }
            else
            {
                // No target date for comparison
                BadgeBrush = UnknownBrush;
                ShowDelta = false;
                TooltipText = $"Predicted completion: {predicted:MMMM d, yyyy}";
            }
        }

        private static string FormatDate(DateTime date)
        {
            var today = DateTime.Today;
            var daysFromNow = (date.Date - today).Days;

            if (daysFromNow == 0)
                return "Today";
            if (daysFromNow == 1)
                return "Tomorrow";
            if (daysFromNow > 0 && daysFromNow < 7)
                return date.ToString("ddd, MMM d");
            if (date.Year == today.Year)
                return date.ToString("MMM d");
            return date.ToString("MMM d, yyyy");
        }

        #endregion
    }
}
