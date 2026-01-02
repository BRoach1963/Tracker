using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A circular progress indicator showing percentage completion.
    /// </summary>
    public partial class ProgressRing : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressRing),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ProgressRing),
                new PropertyMetadata(100.0, OnValueChanged));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(ProgressRing),
                new PropertyMetadata(60.0, OnSizeChanged));

        public static readonly DependencyProperty StrokeWidthProperty =
            DependencyProperty.Register(nameof(StrokeWidth), typeof(double), typeof(ProgressRing),
                new PropertyMetadata(6.0, OnSizeChanged));

        public static readonly DependencyProperty ProgressColorProperty =
            DependencyProperty.Register(nameof(ProgressColor), typeof(Brush), typeof(ProgressRing),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(76, 175, 80)))); // Green

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ProgressRing),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(nameof(ShowLabel), typeof(bool), typeof(ProgressRing),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ShowPercentProperty =
            DependencyProperty.Register(nameof(ShowPercent), typeof(bool), typeof(ProgressRing),
                new PropertyMetadata(true, OnShowPercentChanged));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ProgressRing),
                new PropertyMetadata(null, OnDisplayTextChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public double StrokeWidth
        {
            get => (double)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public Brush ProgressColor
        {
            get => (Brush)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        public bool ShowPercent
        {
            get => (bool)GetValue(ShowPercentProperty);
            set => SetValue(ShowPercentProperty, value);
        }

        public string? DisplayText
        {
            get => (string?)GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        // Computed font size based on ring size
        public new double FontSize => Size / 4.5;

        #endregion

        public ProgressRing()
        {
            InitializeComponent();
            UpdateArc();
        }

        #region Property Changed Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing ring)
            {
                ring.UpdateArc();
                ring.UpdateText();
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing ring)
            {
                ring.UpdateArc();
            }
        }

        private static void OnShowPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing ring)
            {
                ring.UpdateText();
            }
        }

        private static void OnDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing ring)
            {
                ring.UpdateText();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateArc()
        {
            if (ProgressArc == null) return;

            double percentage = Maximum > 0 ? Value / Maximum : 0;
            percentage = Math.Max(0, Math.Min(1, percentage));

            double radius = (Size - StrokeWidth) / 2;
            double centerX = Size / 2;
            double centerY = Size / 2;

            // Start at top (12 o'clock position)
            double startAngle = -90;
            double endAngle = startAngle + (percentage * 360);

            if (percentage >= 1)
            {
                // Full circle - use ellipse geometry
                ProgressArc.Data = new EllipseGeometry(new Point(centerX, centerY), radius, radius);
            }
            else if (percentage > 0)
            {
                // Partial arc
                double startRad = startAngle * Math.PI / 180;
                double endRad = endAngle * Math.PI / 180;

                double startX = centerX + radius * Math.Cos(startRad);
                double startY = centerY + radius * Math.Sin(startRad);
                double endX = centerX + radius * Math.Cos(endRad);
                double endY = centerY + radius * Math.Sin(endRad);

                bool isLargeArc = percentage > 0.5;

                var figure = new PathFigure
                {
                    StartPoint = new Point(startX, startY),
                    IsClosed = false
                };

                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(endX, endY),
                    Size = new Size(radius, radius),
                    IsLargeArc = isLargeArc,
                    SweepDirection = SweepDirection.Clockwise
                });

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                ProgressArc.Data = geometry;
            }
            else
            {
                ProgressArc.Data = null;
            }
        }

        private void UpdateText()
        {
            if (PercentText == null) return;

            if (!string.IsNullOrEmpty(DisplayText))
            {
                PercentText.Text = DisplayText;
            }
            else if (ShowPercent)
            {
                double percentage = Maximum > 0 ? (Value / Maximum) * 100 : 0;
                PercentText.Text = $"{percentage:0}%";
            }
            else
            {
                PercentText.Text = $"{Value:0}/{Maximum:0}";
            }
        }

        #endregion
    }
}
