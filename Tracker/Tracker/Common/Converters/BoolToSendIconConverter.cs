using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a boolean (IsTeamsMessage) to the appropriate send icon.
    /// True = Teams icon, False = Email icon
    /// </summary>
    public class BoolToSendIconConverter : IValueConverter
    {
        public static readonly BoolToSendIconConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTeams)
            {
                return isTeams ? "💬" : "📧";
            }
            return "➤";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

