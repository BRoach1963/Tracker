using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.Google
{
    /// <summary>
    /// Handles Google People API operations for profile photos and contact information.
    /// </summary>
    public class GooglePeopleService
    {
        #region Singleton

        private static GooglePeopleService? _instance;
        private static readonly object _lock = new();

        public static GooglePeopleService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GooglePeopleService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private PeopleServiceService? _service;
        private readonly Dictionary<string, CachedPhoto> _photoCache = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);

        #endregion

        #region Constructor

        private GooglePeopleService()
        {
            _logger = LoggingManager.GetComponentLogger("GooglePeople");
        }

        #endregion

        #region Initialization

        private async Task<bool> EnsureServiceAsync()
        {
            if (_service != null) return true;

            if (!GoogleAuthService.Instance.IsAuthenticated)
            {
                var success = await GoogleAuthService.Instance.TrySilentSignInAsync();
                if (!success) return false;
            }

            _service = new PeopleServiceService(GoogleAuthService.Instance.GetServiceInitializer());
            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current user's profile photo.
        /// </summary>
        public async Task<ImageSource?> GetMyPhotoAsync()
        {
            if (!await EnsureServiceAsync()) return null;

            try
            {
                var request = _service!.People.Get("people/me");
                request.PersonFields = "photos";

                var person = await request.ExecuteAsync();
                var photoUrl = person.Photos?.FirstOrDefault()?.Url;

                if (!string.IsNullOrEmpty(photoUrl))
                {
                    return await DownloadImageAsync(photoUrl);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get user photo");
                return null;
            }
        }

        /// <summary>
        /// Gets a contact's profile photo by email address.
        /// </summary>
        /// <param name="email">The email address to search for</param>
        /// <returns>The profile photo as an ImageSource, or null if not found</returns>
        public async Task<ImageSource?> GetProfilePhotoByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;

            // Check cache first
            if (_photoCache.TryGetValue(email.ToLower(), out var cached))
            {
                if (DateTime.UtcNow - cached.Timestamp < _cacheExpiry)
                {
                    return cached.Image;
                }
                _photoCache.Remove(email.ToLower());
            }

            if (!await EnsureServiceAsync()) return null;

            try
            {
                // Search for the contact by email
                var request = _service!.People.SearchContacts();
                request.Query = email;
                request.ReadMask = "photos,emailAddresses";
                request.PageSize = 10;

                var response = await request.ExecuteAsync();
                var person = response.Results?.FirstOrDefault(r => 
                    r.Person?.EmailAddresses?.Any(e => 
                        e.Value?.Equals(email, StringComparison.OrdinalIgnoreCase) == true) == true)?.Person;

                if (person?.Photos?.Any() == true)
                {
                    var photoUrl = person.Photos.First().Url;
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        var image = await DownloadImageAsync(photoUrl);
                        
                        // Cache the result
                        _photoCache[email.ToLower()] = new CachedPhoto
                        {
                            Image = image,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        return image;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Debug($"No photo found for {email}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches directory for users (Google Workspace only).
        /// </summary>
        public async Task<List<Person>?> SearchDirectoryAsync(string query, int maxResults = 10)
        {
            if (!await EnsureServiceAsync()) return null;

            try
            {
                var request = _service!.People.SearchDirectoryPeople();
                request.Query = query;
                request.ReadMask = "names,emailAddresses,photos,organizations";
                request.PageSize = maxResults;
                request.Sources = PeopleResource.SearchDirectoryPeopleRequest.SourcesEnum.DIRECTORYSOURCETYPEDOMAINPROFILE;

                var response = await request.ExecuteAsync();
                return response.People?.ToList() ?? new List<Person>();
            }
            catch (Exception ex)
            {
                // Directory search may not be available for personal accounts
                _logger.Debug($"Directory search not available: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets photos for multiple email addresses in batch.
        /// </summary>
        public async Task<Dictionary<string, ImageSource?>> GetPhotosForEmailsAsync(IEnumerable<string> emails)
        {
            var results = new Dictionary<string, ImageSource?>();
            
            foreach (var email in emails.Distinct())
            {
                if (string.IsNullOrEmpty(email)) continue;
                
                try
                {
                    var photo = await GetProfilePhotoByEmailAsync(email);
                    results[email] = photo;
                }
                catch
                {
                    results[email] = null;
                }
            }
            
            return results;
        }

        #endregion

        #region Private Methods

        private async Task<ImageSource?> DownloadImageAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var token = await GoogleAuthService.Instance.GetAccessTokenAsync();
                
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var imageBytes = await httpClient.GetByteArrayAsync(url);
                
                return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new BitmapImage();
                    using var stream = new MemoryStream(imageBytes);
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return (ImageSource)bitmap;
                });
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to download image: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Internal Classes

        private class CachedPhoto
        {
            public ImageSource? Image { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}

