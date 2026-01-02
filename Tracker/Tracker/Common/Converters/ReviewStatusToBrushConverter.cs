using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts review status enum values to appropriate brush colors for status badges.
    /// </summary>
    public class ReviewStatusToBrushConverter : IValueConverter
    {
        // Singleton instance
        public static readonly ReviewStatusToBrushConverter Instance = new();

        // Status colors - Material Design inspired
        private static readonly SolidColorBrush NotStartedBrush = new(Color.FromRgb(158, 158, 158)); // Grey
        private static readonly SolidColorBrush DraftBrush = new(Color.FromRgb(158, 158, 158)); // Grey
        private static readonly SolidColorBrush InProgressBrush = new(Color.FromRgb(33, 150, 243)); // Blue
        private static readonly SolidColorBrush CompleteBrush = new(Color.FromRgb(156, 39, 176)); // Purple
        private static readonly SolidColorBrush SharedBrush = new(Color.FromRgb(76, 175, 80)); // Green
        private static readonly SolidColorBrush DiscussedBrush = new(Color.FromRgb(0, 150, 136)); // Teal
        private static readonly SolidColorBrush ArchivedBrush = new(Color.FromRgb(96, 125, 139)); // Blue Grey
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(96, 125, 139)); // Blue Grey

        static ReviewStatusToBrushConverter()
        {
            NotStartedBrush.Freeze();
            DraftBrush.Freeze();
            InProgressBrush.Freeze();
            CompleteBrush.Freeze();
            SharedBrush.Freeze();
            DiscussedBrush.Freeze();
            ArchivedBrush.Freeze();
            DefaultBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReviewStatus status)
            {
                return status switch
                {
                    ReviewStatus.NotStarted => NotStartedBrush,
                    ReviewStatus.SelfReviewInProgress => InProgressBrush,
                    ReviewStatus.SelfReviewComplete => CompleteBrush,
                    ReviewStatus.ManagerReviewInProgress => InProgressBrush,
                    ReviewStatus.ManagerReviewComplete => CompleteBrush,
                    ReviewStatus.Shared => SharedBrush,
                    ReviewStatus.Discussed => DiscussedBrush,
                    _ => DefaultBrush
                };
            }

            if (value is ReviewCycleStatus cycleStatus)
            {
                return cycleStatus switch
                {
                    ReviewCycleStatus.Draft => DraftBrush,
                    ReviewCycleStatus.SelfReviewInProgress => InProgressBrush,
                    ReviewCycleStatus.ManagerReviewInProgress => InProgressBrush,
                    ReviewCycleStatus.Calibration => CompleteBrush,
                    ReviewCycleStatus.Completed => SharedBrush,
                    ReviewCycleStatus.Archived => ArchivedBrush,
                    _ => DefaultBrush
                };
            }

            // String-based fallback
            if (value is string statusText)
            {
                return statusText.ToLowerInvariant() switch
                {
                    "draft" or "not started" => DraftBrush,
                    "in progress" or "inprogress" => InProgressBrush,
                    "complete" => CompleteBrush,
                    "completed" or "shared" => SharedBrush,
                    "discussed" => DiscussedBrush,
                    "archived" => ArchivedBrush,
                    _ => DefaultBrush
                };
            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts review status to a foreground color (white or dark) for contrast.
    /// </summary>
    public class ReviewStatusToForegroundConverter : IValueConverter
    {
        public static readonly ReviewStatusToForegroundConverter Instance = new();

        private static readonly SolidColorBrush WhiteBrush = new(Colors.White);
        private static readonly SolidColorBrush DarkBrush = new(Color.FromRgb(33, 33, 33));

        static ReviewStatusToForegroundConverter()
        {
            WhiteBrush.Freeze();
            DarkBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // All our status colors are dark enough to use white text
            return WhiteBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a status to an appropriate Material Design icon path.
    /// </summary>
    public class ReviewStatusToIconConverter : IValueConverter
    {
        public static readonly ReviewStatusToIconConverter Instance = new();

        // Material Design icons
        private const string NotStartedIcon = "M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.5 2,12A10,10 0 0,1 12,2"; // circle outline
        private const string DraftIcon = "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M19,19H5V5H19V19M17,17H7V7H17V17Z"; // edit square outline
        private const string InProgressIcon = "M12,4V2A10,10 0 0,0 2,12H4A8,8 0 0,1 12,4Z"; // loading/progress
        private const string CompleteIcon = "M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z"; // check
        private const string SharedIcon = "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z"; // check circle
        private const string DiscussedIcon = "M12,3C17.5,3 22,6.58 22,11C22,15.42 17.5,19 12,19C10.76,19 9.57,18.82 8.47,18.5C5.55,21 2,21 2,21C4.33,18.67 4.7,17.1 4.75,16.5C3.05,15.07 2,13.13 2,11C2,6.58 6.5,3 12,3Z"; // chat bubble
        private const string ArchivedIcon = "M3,3H21V7H3V3M4,8H20V21H4V8M9.5,11A0.5,0.5 0 0,0 9,11.5V13H15V11.5A0.5,0.5 0 0,0 14.5,11H9.5Z"; // archive

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReviewStatus status)
            {
                return status switch
                {
                    ReviewStatus.NotStarted => Geometry.Parse(NotStartedIcon),
                    ReviewStatus.SelfReviewInProgress or ReviewStatus.ManagerReviewInProgress => Geometry.Parse(InProgressIcon),
                    ReviewStatus.SelfReviewComplete or ReviewStatus.ManagerReviewComplete => Geometry.Parse(CompleteIcon),
                    ReviewStatus.Shared => Geometry.Parse(SharedIcon),
                    ReviewStatus.Discussed => Geometry.Parse(DiscussedIcon),
                    _ => Geometry.Parse(NotStartedIcon)
                };
            }

            if (value is ReviewCycleStatus cycleStatus)
            {
                return cycleStatus switch
                {
                    ReviewCycleStatus.Draft => Geometry.Parse(DraftIcon),
                    ReviewCycleStatus.SelfReviewInProgress or ReviewCycleStatus.ManagerReviewInProgress => Geometry.Parse(InProgressIcon),
                    ReviewCycleStatus.Calibration => Geometry.Parse(DiscussedIcon),
                    ReviewCycleStatus.Completed => Geometry.Parse(SharedIcon),
                    ReviewCycleStatus.Archived => Geometry.Parse(ArchivedIcon),
                    _ => Geometry.Parse(DraftIcon)
                };
            }

            return Geometry.Parse(DraftIcon);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
