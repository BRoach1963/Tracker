using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts progress value, maximum, and container width to a fill width.
    /// 
    /// Usage:
    /// <code>
    /// &lt;Border.Width&gt;
    ///     &lt;MultiBinding Converter="{StaticResource ProgressWidthConverter}"&gt;
    ///         &lt;Binding Path="Value"/&gt;
    ///         &lt;Binding Path="Maximum"/&gt;
    ///         &lt;Binding Path="ActualWidth" ElementName="Container"/&gt;
    ///     &lt;/MultiBinding&gt;
    /// &lt;/Border.Width&gt;
    /// </code>
    /// </summary>
    public class ProgressWidthConverter : IMultiValueConverter
    {
        /// <summary>
        /// Singleton instance for XAML usage.
        /// </summary>
        public static readonly ProgressWidthConverter Instance = new();

        /// <summary>
        /// Converts value/maximum/width to a proportional width.
        /// </summary>
        /// <param name="values">Array of [value, maximum, containerWidth]</param>
        /// <param name="targetType">Target type (double for width).</param>
        /// <param name="parameter">Optional parameter (unused).</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>The calculated width, or 0 if conversion fails.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return 0d;

            // Handle unset values during binding
            if (values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue ||
                values[2] == DependencyProperty.UnsetValue)
                return 0d;

            try
            {
                var value = System.Convert.ToDouble(values[0]);
                var maximum = System.Convert.ToDouble(values[1]);
                var containerWidth = System.Convert.ToDouble(values[2]);

                if (maximum <= 0 || containerWidth <= 0)
                    return 0d;

                var percentage = Math.Min(1.0, Math.Max(0, value / maximum));
                return percentage * containerWidth;
            }
            catch
            {
                return 0d;
            }
        }

        /// <summary>
        /// Not implemented - one-way binding only.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

