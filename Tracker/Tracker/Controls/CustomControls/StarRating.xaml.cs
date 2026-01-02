using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A clickable star rating control (1-5 stars) with hover effects.
    /// </summary>
    public partial class StarRating : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int?),
                typeof(StarRating),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(StarRating),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public static readonly DependencyProperty StarColorProperty =
            DependencyProperty.Register(
                nameof(StarColor),
                typeof(Brush),
                typeof(StarRating),
                new PropertyMetadata(null, OnStarColorChanged));

        public int? Value
        {
            get => (int?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public Brush? StarColor
        {
            get => (Brush?)GetValue(StarColorProperty);
            set => SetValue(StarColorProperty, value);
        }

        #endregion

        #region Fields

        private readonly Path[] _stars;
        private readonly string[] _ratingLabels = new[]
        {
            "Not rated",
            "Needs Improvement",
            "Developing",
            "Meets Expectations",
            "Exceeds Expectations",
            "Outstanding"
        };

        private static readonly Geometry _filledStar = Geometry.Parse("M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z");
        private static readonly Geometry _outlineStar = Geometry.Parse("M12,15.39L8.24,17.66L9.23,13.38L5.91,10.5L10.29,10.13L12,6.09L13.71,10.13L18.09,10.5L14.77,13.38L15.76,17.66M22,9.24L14.81,8.63L12,2L9.19,8.63L2,9.24L7.45,13.97L5.82,21L12,17.27L18.18,21L16.54,13.97L22,9.24Z");

        #endregion

        public StarRating()
        {
            InitializeComponent();
            _stars = new[] { Star1, Star2, Star3, Star4, Star5 };

            // Wire up hover events for preview
            foreach (var button in GetStarButtons())
            {
                button.MouseEnter += StarButton_MouseEnter;
                button.MouseLeave += StarButton_MouseLeave;
            }
        }

        #region Event Handlers

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;

            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int rating))
            {
                // Toggle off if clicking the same value
                Value = (Value == rating) ? null : rating;
            }
        }

        private void StarButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsReadOnly) return;

            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int hoverRating))
            {
                UpdateStarsVisual(hoverRating, isPreview: true);
            }
        }

        private void StarButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsReadOnly) return;

            // Restore to actual value
            UpdateStarsVisual(Value ?? 0, isPreview: false);
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StarRating control)
            {
                control.UpdateStarsVisual(control.Value ?? 0, isPreview: false);
                control.UpdateRatingLabel();
            }
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StarRating control)
            {
                foreach (var button in control.GetStarButtons())
                {
                    button.IsEnabled = !(bool)e.NewValue;
                    button.Cursor = (bool)e.NewValue 
                        ? System.Windows.Input.Cursors.Arrow 
                        : System.Windows.Input.Cursors.Hand;
                }
            }
        }

        private static void OnStarColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StarRating control)
            {
                control.UpdateStarsVisual(control.Value ?? 0, isPreview: false);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateStarsVisual(int rating, bool isPreview)
        {
            var filledColor = StarColor ?? GetGoldBrush();
            var emptyColor = (Brush)FindResource("HintTextBrush");
            var previewColor = isPreview ? GetPreviewBrush() : filledColor;

            for (int i = 0; i < _stars.Length; i++)
            {
                bool isFilled = (i + 1) <= rating;
                _stars[i].Data = isFilled ? _filledStar : _outlineStar;
                _stars[i].Fill = isFilled ? (isPreview ? previewColor : filledColor) : emptyColor;
            }
        }

        private void UpdateRatingLabel()
        {
            int index = Value.HasValue && Value.Value >= 1 && Value.Value <= 5 ? Value.Value : 0;
            RatingLabel.Text = _ratingLabels[index];
            RatingLabel.Foreground = Value.HasValue 
                ? (Brush)FindResource("ForegroundBrush") 
                : (Brush)FindResource("HintTextBrush");
        }

        private IEnumerable<Button> GetStarButtons()
        {
            return _stars.Select(s => s.Parent as Button).Where(b => b != null)!;
        }

        private Brush GetGoldBrush()
        {
            // Amber/Gold color for filled stars
            return new SolidColorBrush(Color.FromRgb(255, 193, 7));
        }

        private Brush GetPreviewBrush()
        {
            // Lighter amber for hover preview
            return new SolidColorBrush(Color.FromRgb(255, 213, 79));
        }

        #endregion
    }
}
