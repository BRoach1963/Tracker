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
    /// Compact sparkline control for inline trend visualization.
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:SparklineControl DataPoints="{Binding ProgressHistory}"/&gt;
    /// &lt;controls:SparklineControl 
    ///     DataPoints="{Binding ProgressHistory}"
    ///     TrendDirection="Improving"
    ///     ChartWidth="80" ChartHeight="24"/&gt;
    /// </code>
    /// </summary>
    public partial class SparklineControl : UserControl
    {
        #region Static Resources

        private static readonly SolidColorBrush ImprovingBrush = new(Color.FromRgb(16, 185, 129));  // #10B981 - Green
        private static readonly SolidColorBrush DecliningBrush = new(Color.FromRgb(239, 68, 68));   // #EF4444 - Red
        private static readonly SolidColorBrush StableBrush = new(Color.FromRgb(107, 114, 128));    // #6B7280 - Gray
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(99, 102, 241));    // #6366F1 - Indigo

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The data points to display (values 0-100).
        /// </summary>
        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(nameof(DataPoints), typeof(IEnumerable<double>), typeof(SparklineControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// Optional trend direction for coloring.
        /// </summary>
        public static readonly DependencyProperty TrendDirectionProperty =
            DependencyProperty.Register(nameof(TrendDirection), typeof(TrendAnalyzer.TrendDirection?), typeof(SparklineControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// Custom line color (overrides trend-based coloring).
        /// </summary>
        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register(nameof(LineColor), typeof(Brush), typeof(SparklineControl),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// Width of the chart area.
        /// </summary>
        public static readonly DependencyProperty ChartWidthProperty =
            DependencyProperty.Register(nameof(ChartWidth), typeof(double), typeof(SparklineControl),
                new PropertyMetadata(80.0, OnSizeChanged));

        /// <summary>
        /// Height of the chart area.
        /// </summary>
        public static readonly DependencyProperty ChartHeightProperty =
            DependencyProperty.Register(nameof(ChartHeight), typeof(double), typeof(SparklineControl),
                new PropertyMetadata(24.0, OnSizeChanged));

        /// <summary>
        /// Thickness of the line.
        /// </summary>
        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(nameof(LineThickness), typeof(double), typeof(SparklineControl),
                new PropertyMetadata(1.5, OnDataChanged));

        /// <summary>
        /// Whether to show the end point dot.
        /// </summary>
        public static readonly DependencyProperty ShowEndPointProperty =
            DependencyProperty.Register(nameof(ShowEndPoint), typeof(bool), typeof(SparklineControl),
                new PropertyMetadata(true, OnDataChanged));

        /// <summary>
        /// Whether to show a fill under the line.
        /// </summary>
        public static readonly DependencyProperty ShowFillProperty =
            DependencyProperty.Register(nameof(ShowFill), typeof(bool), typeof(SparklineControl),
                new PropertyMetadata(false, OnDataChanged));

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(SparklineControl),
                new PropertyMetadata("Progress trend"));

        #endregion

        #region Properties

        public IEnumerable<double> DataPoints
        {
            get => (IEnumerable<double>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        public TrendAnalyzer.TrendDirection? TrendDirection
        {
            get => (TrendAnalyzer.TrendDirection?)GetValue(TrendDirectionProperty);
            set => SetValue(TrendDirectionProperty, value);
        }

        public Brush LineColor
        {
            get => (Brush)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public double ChartWidth
        {
            get => (double)GetValue(ChartWidthProperty);
            set => SetValue(ChartWidthProperty, value);
        }

        public double ChartHeight
        {
            get => (double)GetValue(ChartHeightProperty);
            set => SetValue(ChartHeightProperty, value);
        }

        public double LineThickness
        {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        public bool ShowEndPoint
        {
            get => (bool)GetValue(ShowEndPointProperty);
            set => SetValue(ShowEndPointProperty, value);
        }

        public bool ShowFill
        {
            get => (bool)GetValue(ShowFillProperty);
            set => SetValue(ShowFillProperty, value);
        }

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            private set => SetValue(TooltipTextProperty, value);
        }

        #endregion

        #region Constructor

        public SparklineControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SparklineControl sparkline)
            {
                sparkline.DrawSparkline();
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SparklineControl sparkline)
            {
                sparkline.DrawSparkline();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets data from progress snapshots.
        /// </summary>
        public void SetDataFromSnapshots(IEnumerable<DataModels.ProgressSnapshot>? snapshots)
        {
            if (snapshots == null)
            {
                DataPoints = null;
                return;
            }

            var values = snapshots
                .OrderBy(s => s.SnapshotDate)
                .Select(s => (double)s.Progress)
                .ToList();

            DataPoints = values;
        }

        /// <summary>
        /// Sets data from trajectory points.
        /// </summary>
        public void SetDataFromTrajectory(IEnumerable<TrajectoryPredictor.TrajectoryPoint>? points, bool historicalOnly = true)
        {
            if (points == null)
            {
                DataPoints = null;
                return;
            }

            var filteredPoints = historicalOnly 
                ? points.Where(p => p.IsHistorical) 
                : points;

            var values = filteredPoints
                .OrderBy(p => p.Date)
                .Select(p => p.ProjectedProgress)
                .ToList();

            DataPoints = values;
        }

        #endregion

        #region Private Methods

        private void DrawSparkline()
        {
            SparklineCanvas.Children.Clear();

            var points = DataPoints?.ToList();
            if (points == null || points.Count < 2)
            {
                TooltipText = "Not enough data";
                return;
            }

            var brush = GetLineBrush();
            var width = ChartWidth;
            var height = ChartHeight;
            var padding = 2.0;

            // Calculate min/max for scaling
            var minValue = Math.Min(0, points.Min());
            var maxValue = Math.Max(100, points.Max());
            var range = maxValue - minValue;
            if (range == 0) range = 1;

            // Calculate points
            var chartPoints = new List<Point>();
            var stepX = (width - padding * 2) / (points.Count - 1);

            for (int i = 0; i < points.Count; i++)
            {
                var x = padding + (i * stepX);
                var normalizedValue = (points[i] - minValue) / range;
                var y = height - padding - (normalizedValue * (height - padding * 2));
                chartPoints.Add(new Point(x, y));
            }

            // Draw fill if enabled
            if (ShowFill && chartPoints.Count >= 2)
            {
                var fillGeometry = CreateFillGeometry(chartPoints, height, padding);
                var fillBrush = brush.Clone();
                fillBrush.Opacity = 0.1;

                var fillPath = new Path
                {
                    Data = fillGeometry,
                    Fill = fillBrush
                };
                SparklineCanvas.Children.Add(fillPath);
            }

            // Draw line
            var lineGeometry = CreateLineGeometry(chartPoints);
            var linePath = new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = LineThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            SparklineCanvas.Children.Add(linePath);

            // Draw end point
            if (ShowEndPoint && chartPoints.Count > 0)
            {
                var lastPoint = chartPoints.Last();
                var dot = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = brush
                };
                Canvas.SetLeft(dot, lastPoint.X - 2);
                Canvas.SetTop(dot, lastPoint.Y - 2);
                SparklineCanvas.Children.Add(dot);
            }

            // Update tooltip
            UpdateTooltip(points);
        }

        private Brush GetLineBrush()
        {
            if (LineColor != null)
                return LineColor;

            if (TrendDirection.HasValue)
            {
                return TrendDirection.Value switch
                {
                    TrendAnalyzer.TrendDirection.Improving => ImprovingBrush,
                    TrendAnalyzer.TrendDirection.Declining => DecliningBrush,
                    TrendAnalyzer.TrendDirection.Stable => StableBrush,
                    _ => DefaultBrush
                };
            }

            // Auto-detect from data
            var points = DataPoints?.ToList();
            if (points != null && points.Count >= 2)
            {
                var firstHalf = points.Take(points.Count / 2).Average();
                var secondHalf = points.Skip(points.Count / 2).Average();
                var change = secondHalf - firstHalf;

                if (change > 2)
                    return ImprovingBrush;
                if (change < -2)
                    return DecliningBrush;
            }

            return DefaultBrush;
        }

        private static StreamGeometry CreateLineGeometry(List<Point> points)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                if (points.Count > 0)
                {
                    ctx.BeginFigure(points[0], false, false);
                    ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private static StreamGeometry CreateFillGeometry(List<Point> points, double height, double padding)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                if (points.Count > 0)
                {
                    // Start at bottom-left
                    ctx.BeginFigure(new Point(points[0].X, height - padding), true, true);
                    
                    // Line up to first point
                    ctx.LineTo(points[0], true, true);
                    
                    // Draw all points
                    ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
                    
                    // Line down to bottom-right
                    ctx.LineTo(new Point(points.Last().X, height - padding), true, true);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private void UpdateTooltip(List<double> points)
        {
            if (points.Count >= 2)
            {
                var first = points.First();
                var last = points.Last();
                var change = last - first;
                var changeText = change >= 0 ? $"+{change:F1}%" : $"{change:F1}%";

                TooltipText = $"Current: {last:F0}%\nChange: {changeText}\nData points: {points.Count}";
            }
            else
            {
                TooltipText = "Not enough data for trend";
            }
        }

        #endregion
    }
}
