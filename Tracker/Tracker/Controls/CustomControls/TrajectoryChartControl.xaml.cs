using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Tracker.Services.Analytics;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// WPF control for visualizing progress trajectory with predictions.
    /// Shows actual progress, expected progress, and projected completion.
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:TrajectoryChartControl 
    ///     TrajectoryPoints="{Binding TrajectoryData}"
    ///     StartDate="{Binding StartDate}"
    ///     TargetDate="{Binding DueDate}"/&gt;
    /// </code>
    /// </summary>
    public partial class TrajectoryChartControl : UserControl
    {
        #region Static Resources

        private static readonly SolidColorBrush ActualBrush = new(Color.FromRgb(99, 102, 241));     // #6366F1 - Indigo
        private static readonly SolidColorBrush ExpectedBrush = new(Color.FromRgb(156, 163, 175));  // #9CA3AF - Gray
        private static readonly SolidColorBrush ProjectedOnTrackBrush = new(Color.FromRgb(16, 185, 129));  // #10B981 - Green
        private static readonly SolidColorBrush ProjectedAtRiskBrush = new(Color.FromRgb(245, 158, 11));   // #F59E0B - Amber
        private static readonly SolidColorBrush ProjectedOffTrackBrush = new(Color.FromRgb(239, 68, 68));  // #EF4444 - Red
        private static readonly SolidColorBrush GridLineBrush = new(Color.FromRgb(229, 231, 235));  // #E5E7EB - Light Gray

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The trajectory points to display.
        /// </summary>
        public static readonly DependencyProperty TrajectoryPointsProperty =
            DependencyProperty.Register(nameof(TrajectoryPoints), typeof(IEnumerable<TrajectoryPredictor.TrajectoryPoint>), 
                typeof(TrajectoryChartControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// The start date of the tracked period.
        /// </summary>
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(nameof(StartDate), typeof(DateTime?), typeof(TrajectoryChartControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// The target/end date of the tracked period.
        /// </summary>
        public static readonly DependencyProperty TargetDateProperty =
            DependencyProperty.Register(nameof(TargetDate), typeof(DateTime?), typeof(TrajectoryChartControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// Risk level for coloring projected line.
        /// </summary>
        public static readonly DependencyProperty RiskLevelProperty =
            DependencyProperty.Register(nameof(RiskLevel), typeof(TrajectoryPredictor.RiskLevel?), typeof(TrajectoryChartControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// Whether to show the legend.
        /// </summary>
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(TrajectoryChartControl),
                new PropertyMetadata(true));

        /// <summary>
        /// Whether to show the today marker.
        /// </summary>
        public static readonly DependencyProperty ShowTodayMarkerProperty =
            DependencyProperty.Register(nameof(ShowTodayMarker), typeof(bool), typeof(TrajectoryChartControl),
                new PropertyMetadata(true));

        /// <summary>
        /// Whether to show grid lines.
        /// </summary>
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register(nameof(ShowGridLines), typeof(bool), typeof(TrajectoryChartControl),
                new PropertyMetadata(true, OnDataChanged));

        // Read-only properties for binding
        public static readonly DependencyProperty ActualLineBrushProperty =
            DependencyProperty.Register(nameof(ActualLineBrush), typeof(Brush), typeof(TrajectoryChartControl),
                new PropertyMetadata(ActualBrush));

        public static readonly DependencyProperty ExpectedLineBrushProperty =
            DependencyProperty.Register(nameof(ExpectedLineBrush), typeof(Brush), typeof(TrajectoryChartControl),
                new PropertyMetadata(ExpectedBrush));

        public static readonly DependencyProperty ProjectedLineBrushProperty =
            DependencyProperty.Register(nameof(ProjectedLineBrush), typeof(Brush), typeof(TrajectoryChartControl),
                new PropertyMetadata(ProjectedOnTrackBrush));

        public static readonly DependencyProperty HasProjectionProperty =
            DependencyProperty.Register(nameof(HasProjection), typeof(bool), typeof(TrajectoryChartControl),
                new PropertyMetadata(false));

        #endregion

        #region Properties

        public IEnumerable<TrajectoryPredictor.TrajectoryPoint> TrajectoryPoints
        {
            get => (IEnumerable<TrajectoryPredictor.TrajectoryPoint>)GetValue(TrajectoryPointsProperty);
            set => SetValue(TrajectoryPointsProperty, value);
        }

        public DateTime? StartDate
        {
            get => (DateTime?)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        public DateTime? TargetDate
        {
            get => (DateTime?)GetValue(TargetDateProperty);
            set => SetValue(TargetDateProperty, value);
        }

        public TrajectoryPredictor.RiskLevel? RiskLevel
        {
            get => (TrajectoryPredictor.RiskLevel?)GetValue(RiskLevelProperty);
            set => SetValue(RiskLevelProperty, value);
        }

        public bool ShowLegend
        {
            get => (bool)GetValue(ShowLegendProperty);
            set => SetValue(ShowLegendProperty, value);
        }

        public bool ShowTodayMarker
        {
            get => (bool)GetValue(ShowTodayMarkerProperty);
            set => SetValue(ShowTodayMarkerProperty, value);
        }

        public bool ShowGridLines
        {
            get => (bool)GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }

        public Brush ActualLineBrush
        {
            get => (Brush)GetValue(ActualLineBrushProperty);
            private set => SetValue(ActualLineBrushProperty, value);
        }

        public Brush ExpectedLineBrush
        {
            get => (Brush)GetValue(ExpectedLineBrushProperty);
            private set => SetValue(ExpectedLineBrushProperty, value);
        }

        public Brush ProjectedLineBrush
        {
            get => (Brush)GetValue(ProjectedLineBrushProperty);
            private set => SetValue(ProjectedLineBrushProperty, value);
        }

        public bool HasProjection
        {
            get => (bool)GetValue(HasProjectionProperty);
            private set => SetValue(HasProjectionProperty, value);
        }

        #endregion

        #region Constructor

        public TrajectoryChartControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawChart();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrajectoryChartControl chart)
            {
                chart.UpdateProjectedBrush();
                chart.DrawChart();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets trajectory data from a prediction result.
        /// </summary>
        public void SetFromPrediction(PredictiveAnalyticsService.PredictionResult prediction, DateTime startDate, DateTime? targetDate)
        {
            if (prediction?.TrajectoryPoints != null)
            {
                TrajectoryPoints = prediction.TrajectoryPoints;
                RiskLevel = prediction.Trajectory?.Risk;
            }
            StartDate = startDate;
            TargetDate = targetDate;
        }

        /// <summary>
        /// Refreshes the chart display.
        /// </summary>
        public void Refresh()
        {
            DrawChart();
        }

        #endregion

        #region Private Methods

        private void UpdateProjectedBrush()
        {
            ProjectedLineBrush = RiskLevel switch
            {
                TrajectoryPredictor.RiskLevel.OnTrack => ProjectedOnTrackBrush,
                TrajectoryPredictor.RiskLevel.AtRisk => ProjectedAtRiskBrush,
                TrajectoryPredictor.RiskLevel.Critical => ProjectedOffTrackBrush,
                _ => ProjectedOnTrackBrush
            };
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();

            var points = TrajectoryPoints?.ToList();
            if (points == null || points.Count == 0)
                return;

            var width = ChartCanvas.ActualWidth;
            var height = ChartCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            // Determine date range
            var minDate = StartDate ?? points.Min(p => p.Date);
            var maxDate = TargetDate ?? points.Max(p => p.Date);
            var dateRange = (maxDate - minDate).TotalDays;
            if (dateRange <= 0) dateRange = 1;

            // Update date labels
            StartDateLabel.Text = minDate.ToString("MMM d");
            EndDateLabel.Text = maxDate.ToString("MMM d");

            // Check for projected data
            HasProjection = points.Any(p => !p.IsHistorical);

            // Draw grid lines
            if (ShowGridLines)
            {
                DrawGridLines(width, height);
            }

            // Draw expected line (linear from 0 to 100)
            DrawExpectedLine(width, height);

            // Separate historical and projected points
            var historicalPoints = points.Where(p => p.IsHistorical).OrderBy(p => p.Date).ToList();
            var projectedPoints = points.Where(p => !p.IsHistorical).OrderBy(p => p.Date).ToList();

            // Draw actual/historical line
            if (historicalPoints.Count >= 2)
            {
                var screenPoints = ConvertToScreenPoints(historicalPoints, minDate, dateRange, width, height);
                DrawLine(screenPoints, ActualBrush, 2, false);
                DrawEndPoint(screenPoints.Last(), ActualBrush);
            }

            // Draw projected line
            if (projectedPoints.Count >= 2)
            {
                // Connect to last historical point
                var allProjected = new List<TrajectoryPredictor.TrajectoryPoint>();
                if (historicalPoints.Count > 0)
                {
                    allProjected.Add(historicalPoints.Last());
                }
                allProjected.AddRange(projectedPoints);

                var screenPoints = ConvertToScreenPoints(allProjected, minDate, dateRange, width, height);
                DrawLine(screenPoints, ProjectedLineBrush, 2, true);
            }

            // Draw today marker
            if (ShowTodayMarker)
            {
                DrawTodayMarker(minDate, dateRange, width, height);
            }
        }

        private void DrawGridLines(double width, double height)
        {
            // Horizontal grid lines at 25%, 50%, 75%
            foreach (var percentage in new[] { 0.25, 0.5, 0.75 })
            {
                var y = height - (percentage * height);
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = GridLineBrush,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                ChartCanvas.Children.Add(line);
            }
        }

        private void DrawExpectedLine(double width, double height)
        {
            var expectedLine = new Line
            {
                X1 = 0,
                Y1 = height,
                X2 = width,
                Y2 = 0,
                Stroke = ExpectedBrush,
                StrokeThickness = 1,
                Opacity = 0.5,
                StrokeDashArray = new DoubleCollection { 6, 3 }
            };
            ChartCanvas.Children.Add(expectedLine);
        }

        private List<Point> ConvertToScreenPoints(
            List<TrajectoryPredictor.TrajectoryPoint> points, 
            DateTime minDate, 
            double dateRange, 
            double width, 
            double height)
        {
            return points.Select(p =>
            {
                var x = ((p.Date - minDate).TotalDays / dateRange) * width;
                var y = height - (p.ProjectedProgress / 100.0 * height);
                return new Point(x, Math.Max(0, Math.Min(height, y)));
            }).ToList();
        }

        private void DrawLine(List<Point> points, Brush brush, double thickness, bool isDashed)
        {
            if (points.Count < 2) return;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
            }
            geometry.Freeze();

            var path = new Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            if (isDashed)
            {
                path.StrokeDashArray = new DoubleCollection { 4, 2 };
            }

            ChartCanvas.Children.Add(path);
        }

        private void DrawEndPoint(Point point, Brush brush)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = brush
            };
            Canvas.SetLeft(dot, point.X - 4);
            Canvas.SetTop(dot, point.Y - 4);
            ChartCanvas.Children.Add(dot);
        }

        private void DrawTodayMarker(DateTime minDate, double dateRange, double width, double height)
        {
            var today = DateTime.Today;
            var daysFromStart = (today - minDate).TotalDays;
            
            if (daysFromStart < 0 || daysFromStart > dateRange)
            {
                TodayMarker.Visibility = Visibility.Collapsed;
                return;
            }

            var x = (daysFromStart / dateRange) * width;
            
            TodayMarker.Visibility = Visibility.Visible;
            TodayMarker.Margin = new Thickness(34 + x, 0, 0, 0);

            // Also draw on canvas
            var markerLine = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = height,
                Stroke = new SolidColorBrush(Color.FromRgb(99, 102, 241)), // Indigo
                StrokeThickness = 1,
                Opacity = 0.5,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            ChartCanvas.Children.Add(markerLine);
        }

        #endregion
    }
}
