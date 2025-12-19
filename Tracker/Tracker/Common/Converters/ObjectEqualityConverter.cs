using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts an object comparison to boolean (for IsSelected bindings).
    /// Returns true if the bound value equals the converter parameter.
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:OkrCard IsSelected="{Binding SelectedOkr, Converter={x:Static converters:ObjectEqualityConverter.Instance}, ConverterParameter={Binding}}"/&gt;
    /// </code>
    /// 
    /// Note: For comparing with DataContext items in ItemsControl, use MultiBinding instead.
    /// </summary>
    public class ObjectEqualityConverter : IValueConverter, IMultiValueConverter
    {
        /// <summary>
        /// Singleton instance for XAML usage.
        /// </summary>
        public static readonly ObjectEqualityConverter Instance = new();

        /// <summary>
        /// Compares the bound value with the parameter.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return true;
            if (value == null || parameter == null) return false;
            return value.Equals(parameter);
        }

        /// <summary>
        /// Not implemented - one-way binding only.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Compares two values from a MultiBinding.
        /// Values[0] = the selected item from ViewModel
        /// Values[1] = the current item from ItemsControl
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            
            var selected = values[0];
            var current = values[1];

            if (selected == null && current == null) return true;
            if (selected == null || current == null) return false;
            
            return selected.Equals(current);
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

