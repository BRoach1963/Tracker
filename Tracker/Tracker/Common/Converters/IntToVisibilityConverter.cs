using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts an integer to Visibility. Shows element if value > 0, hides otherwise.
    /// 
    /// Usage:
    /// <code>
    /// &lt;Border Visibility="{Binding Count, Converter={x:Static converters:IntToVisibilityConverter.Instance}}"/&gt;
    /// </code>
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for XAML usage.
        /// </summary>
        public static readonly IntToVisibilityConverter Instance = new();

        /// <summary>
        /// Inverse instance - hides when value > 0.
        /// </summary>
        public static readonly IntToVisibilityConverter InverseInstance = new() { Inverse = true };

        /// <summary>
        /// Whether to invert the logic (hide when > 0).
        /// </summary>
        public bool Inverse { get; set; }

        /// <summary>
        /// Converts an integer to Visibility.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <param name="targetType">Target type (Visibility).</param>
        /// <param name="parameter">Optional threshold (default 0).</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Visible if value > threshold, Collapsed otherwise (inverted if Inverse=true).</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = 0;
            var threshold = 0;

            if (value != null)
            {
                try
                {
                    intValue = System.Convert.ToInt32(value);
                }
                catch
                {
                    // Default to 0
                }
            }

            if (parameter != null)
            {
                try
                {
                    threshold = System.Convert.ToInt32(parameter);
                }
                catch
                {
                    // Default to 0
                }
            }

            var isVisible = intValue > threshold;
            if (Inverse) isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Not implemented - one-way binding only.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
