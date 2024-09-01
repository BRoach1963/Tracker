using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections;

namespace Tracker.Helpers
{
    public static class ImageHelper
    {
        public static ImageSource? LoadDefaultProfileImage()
        {
            // Load the embedded resource image
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Tracker.Images.profile.png"; // Adjust namespace as needed

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                BitmapImage? bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }

            return null; // Handle the case where the image can't be loaded
        }

        public static ImageSource? GetImageSourceFromFile(string filePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImageSource imageSource = bitmap;  
            return imageSource;
        }

        public static async Task<ImageSource?> GetImageSourceFromByteArrayAsync(byte[] array)
        {
            if (array == null || array.Length == 0)
                return null;

            BitmapImage bitmap = new BitmapImage();

            using (MemoryStream stream = new MemoryStream(array))
            {
                stream.Position = 0;
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            await bitmap.Dispatcher.InvokeAsync(() =>
            {
                bitmap.Freeze(); // Freezes the BitmapImage to make it cross-thread accessible.
            });

            ImageSource? imageSource = bitmap;
            return imageSource;
        }

        public static byte[] GetByteArrayFromFile(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }
    }
}
