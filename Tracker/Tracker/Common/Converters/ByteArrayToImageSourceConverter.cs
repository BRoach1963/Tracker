using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a byte array to an ImageSource for display in XAML.
    /// Returns null if the byte array is null or empty.
    /// </summary>
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        public static readonly ByteArrayToImageSourceConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0)
                return null;

            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(bytes))
                {
                    stream.Position = 0;
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns Visible if the byte array has data, Collapsed otherwise.
    /// Used to show/hide profile images.
    /// </summary>
    public class ByteArrayToVisibilityConverter : IValueConverter
    {
        public static readonly ByteArrayToVisibilityConverter Instance = new();
        public static readonly ByteArrayToVisibilityConverter InverseInstance = new() { Inverse = true };

        public bool Inverse { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var hasData = value is byte[] bytes && bytes.Length > 0;
            
            if (Inverse)
                hasData = !hasData;

            return hasData ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


