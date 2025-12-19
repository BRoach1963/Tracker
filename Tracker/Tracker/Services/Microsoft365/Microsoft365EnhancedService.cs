using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Enhanced Microsoft 365 features: Teams Meeting Links, Profile Photos, Presence.
    /// </summary>
    public class Microsoft365EnhancedService : IDisposable
    {
        #region Singleton

        private static Microsoft365EnhancedService? _instance;
        private static readonly object _lock = new();

        public static Microsoft365EnhancedService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Microsoft365EnhancedService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

        // Cache for presence to avoid excessive API calls
        private readonly Dictionary<string, (PresenceStatus Status, DateTime CachedAt)> _presenceCache = new();
        private readonly TimeSpan _presenceCacheExpiry = TimeSpan.FromMinutes(2);

        // Cache for profile photos
        private readonly Dictionary<string, (ImageSource? Photo, DateTime CachedAt)> _photoCache = new();
        private readonly TimeSpan _photoCacheExpiry = TimeSpan.FromHours(24);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        #endregion

        #region Constructor

        private Microsoft365EnhancedService()
        {
            _logger = LoggingManager.GetComponentLogger("M365Enhanced");
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(GraphBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #endregion

        #region Teams Meeting Links

        /// <summary>
        /// Creates a Teams online meeting and returns the join URL.
        /// </summary>
        /// <param name="subject">Meeting subject</param>
        /// <param name="startTime">Meeting start time (UTC)</param>
        /// <param name="endTime">Meeting end time (UTC)</param>
        /// <param name="attendeeEmail">Optional attendee email</param>
        /// <returns>Teams meeting join URL, or null if failed</returns>
        public async Task<TeamsMeetingInfo?> CreateTeamsMeetingAsync(
            string subject,
            DateTime startTime,
            DateTime endTime,
            string? attendeeEmail = null)
        {
            if (!MicrosoftGraphAuthService.Instance.IsAuthenticated)
            {
                _logger.Warn("Cannot create Teams meeting - not authenticated");
                return null;
            }

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var meetingPayload = new
                {
                    subject = subject,
                    startDateTime = startTime.ToUniversalTime().ToString("o"),
                    endDateTime = endTime.ToUniversalTime().ToString("o"),
                    participants = attendeeEmail != null ? new
                    {
                        attendees = new[]
                        {
                            new
                            {
                                upn = attendeeEmail,
                                role = "attendee"
                            }
                        }
                    } : null
                };

                var json = JsonSerializer.Serialize(meetingPayload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/me/onlineMeetings", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    var meetingInfo = new TeamsMeetingInfo
                    {
                        JoinUrl = root.TryGetProperty("joinWebUrl", out var joinUrl) ? joinUrl.GetString() : null,
                        MeetingId = root.TryGetProperty("id", out var id) ? id.GetString() : null,
                        JoinInfo = root.TryGetProperty("joinInformation", out var joinInfo) && 
                                   joinInfo.TryGetProperty("content", out var infoContent) 
                                   ? infoContent.GetString() : null
                    };

                    _logger.Info($"Created Teams meeting: {subject}");
                    return meetingInfo;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.Error($"Failed to create Teams meeting: {response.StatusCode} - {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating Teams meeting");
                return null;
            }
        }

        #endregion

        #region Profile Photos

        /// <summary>
        /// Gets a user's profile photo from Azure AD by email.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>ImageSource for the photo, or null if not available</returns>
        public async Task<ImageSource?> GetProfilePhotoAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            // Check cache first
            if (_photoCache.TryGetValue(email.ToLower(), out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < _photoCacheExpiry)
                    return cached.Photo;
            }

            if (!MicrosoftGraphAuthService.Instance.IsAuthenticated)
                return null;

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Try to get the photo content directly
                var response = await _httpClient.GetAsync($"/users/{email}/photo/$value");

                if (response.IsSuccessStatusCode)
                {
                    var photoBytes = await response.Content.ReadAsByteArrayAsync();
                    var image = BytesToImageSource(photoBytes);
                    
                    // Cache the result
                    _photoCache[email.ToLower()] = (image, DateTime.UtcNow);
                    
                    _logger.Debug($"Retrieved profile photo for {email}");
                    return image;
                }

                // Photo not found - cache null to avoid repeated requests
                _photoCache[email.ToLower()] = (null, DateTime.UtcNow);
                _logger.Debug($"No profile photo found for {email}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Error getting profile photo for {email}");
                return null;
            }
        }

        /// <summary>
        /// Gets profile photos for multiple users efficiently.
        /// </summary>
        public async Task<Dictionary<string, ImageSource?>> GetProfilePhotosAsync(IEnumerable<string> emails)
        {
            var results = new Dictionary<string, ImageSource?>();
            var tasks = emails.Select(async email =>
            {
                var photo = await GetProfilePhotoAsync(email);
                return (email, photo);
            });

            var completed = await Task.WhenAll(tasks);
            foreach (var (email, photo) in completed)
            {
                results[email] = photo;
            }

            return results;
        }

        private static ImageSource? BytesToImageSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                var image = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(imageData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze(); // Important for cross-thread access
                }
                return image;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Presence / Availability

        /// <summary>
        /// Gets the presence/availability status for a user.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>Presence status</returns>
        public async Task<PresenceStatus> GetPresenceAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return PresenceStatus.Unknown;

            // Check cache first
            if (_presenceCache.TryGetValue(email.ToLower(), out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < _presenceCacheExpiry)
                    return cached.Status;
            }

            if (!MicrosoftGraphAuthService.Instance.IsAuthenticated)
                return PresenceStatus.Unknown;

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return PresenceStatus.Unknown;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"/users/{email}/presence");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    var availability = root.TryGetProperty("availability", out var avail) 
                        ? avail.GetString() : null;
                    var activity = root.TryGetProperty("activity", out var act) 
                        ? act.GetString() : null;

                    var status = MapPresence(availability, activity);
                    
                    // Cache the result
                    _presenceCache[email.ToLower()] = (status, DateTime.UtcNow);
                    
                    return status;
                }

                _logger.Debug($"Could not get presence for {email}: {response.StatusCode}");
                return PresenceStatus.Unknown;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Error getting presence for {email}");
                return PresenceStatus.Unknown;
            }
        }

        /// <summary>
        /// Gets presence for multiple users in a single batch request.
        /// </summary>
        public async Task<Dictionary<string, PresenceStatus>> GetPresenceBatchAsync(IEnumerable<string> emails)
        {
            var results = new Dictionary<string, PresenceStatus>();
            var emailList = emails.ToList();

            if (!emailList.Any())
                return results;

            // Check cache first, collect uncached
            var uncached = new List<string>();
            foreach (var email in emailList)
            {
                if (_presenceCache.TryGetValue(email.ToLower(), out var cached) &&
                    DateTime.UtcNow - cached.CachedAt < _presenceCacheExpiry)
                {
                    results[email] = cached.Status;
                }
                else
                {
                    uncached.Add(email);
                }
            }

            if (!uncached.Any() || !MicrosoftGraphAuthService.Instance.IsAuthenticated)
                return results;

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return results;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Use batch request for efficiency (up to 650 users)
                var batchPayload = new
                {
                    ids = uncached.Take(650).ToArray()
                };

                var json = JsonSerializer.Serialize(batchPayload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/communications/getPresencesByUserId", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);

                    if (doc.RootElement.TryGetProperty("value", out var values))
                    {
                        foreach (var item in values.EnumerateArray())
                        {
                            var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                            var availability = item.TryGetProperty("availability", out var avail) 
                                ? avail.GetString() : null;
                            var activity = item.TryGetProperty("activity", out var act) 
                                ? act.GetString() : null;

                            if (!string.IsNullOrEmpty(id))
                            {
                                var status = MapPresence(availability, activity);
                                results[id] = status;
                                _presenceCache[id.ToLower()] = (status, DateTime.UtcNow);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting batch presence");
            }

            // Fill in any missing with Unknown
            foreach (var email in emailList)
            {
                if (!results.ContainsKey(email))
                    results[email] = PresenceStatus.Unknown;
            }

            return results;
        }

        private static PresenceStatus MapPresence(string? availability, string? activity)
        {
            return availability?.ToLower() switch
            {
                "available" => PresenceStatus.Available,
                "busy" => activity?.ToLower() switch
                {
                    "inacall" => PresenceStatus.InACall,
                    "inaconferencecall" => PresenceStatus.InACall,
                    "inaconference" => PresenceStatus.InAMeeting,
                    "inameeting" => PresenceStatus.InAMeeting,
                    "presenting" => PresenceStatus.Presenting,
                    _ => PresenceStatus.Busy
                },
                "donotdisturb" => PresenceStatus.DoNotDisturb,
                "away" => PresenceStatus.Away,
                "berightback" => PresenceStatus.BeRightBack,
                "offline" => PresenceStatus.Offline,
                "outofoffice" => PresenceStatus.OutOfOffice,
                _ => PresenceStatus.Unknown
            };
        }

        /// <summary>
        /// Clears the presence cache to force refresh.
        /// </summary>
        public void ClearPresenceCache()
        {
            _presenceCache.Clear();
        }

        /// <summary>
        /// Clears the photo cache.
        /// </summary>
        public void ClearPhotoCache()
        {
            _photoCache.Clear();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// Information about a created Teams meeting.
    /// </summary>
    public class TeamsMeetingInfo
    {
        public string? JoinUrl { get; set; }
        public string? MeetingId { get; set; }
        public string? JoinInfo { get; set; }
    }

    /// <summary>
    /// User presence/availability status.
    /// </summary>
    public enum PresenceStatus
    {
        Unknown,
        Available,
        Busy,
        InACall,
        InAMeeting,
        Presenting,
        DoNotDisturb,
        Away,
        BeRightBack,
        Offline,
        OutOfOffice
    }

    /// <summary>
    /// Extension methods for PresenceStatus.
    /// </summary>
    public static class PresenceStatusExtensions
    {
        /// <summary>
        /// Gets the emoji indicator for the presence status.
        /// </summary>
        public static string ToEmoji(this PresenceStatus status) => status switch
        {
            PresenceStatus.Available => "🟢",
            PresenceStatus.Busy => "🔴",
            PresenceStatus.InACall => "📞",
            PresenceStatus.InAMeeting => "📅",
            PresenceStatus.Presenting => "📺",
            PresenceStatus.DoNotDisturb => "⛔",
            PresenceStatus.Away => "🟡",
            PresenceStatus.BeRightBack => "🟡",
            PresenceStatus.Offline => "⚫",
            PresenceStatus.OutOfOffice => "🏖️",
            _ => "⚪"
        };

        /// <summary>
        /// Gets a human-readable display string.
        /// </summary>
        public static string ToDisplayString(this PresenceStatus status) => status switch
        {
            PresenceStatus.Available => "Available",
            PresenceStatus.Busy => "Busy",
            PresenceStatus.InACall => "In a call",
            PresenceStatus.InAMeeting => "In a meeting",
            PresenceStatus.Presenting => "Presenting",
            PresenceStatus.DoNotDisturb => "Do not disturb",
            PresenceStatus.Away => "Away",
            PresenceStatus.BeRightBack => "Be right back",
            PresenceStatus.Offline => "Offline",
            PresenceStatus.OutOfOffice => "Out of office",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets the color brush for the presence status.
        /// </summary>
        public static string ToColorHex(this PresenceStatus status) => status switch
        {
            PresenceStatus.Available => "#28A745",
            PresenceStatus.Busy or PresenceStatus.InACall or PresenceStatus.InAMeeting => "#DC3545",
            PresenceStatus.Presenting or PresenceStatus.DoNotDisturb => "#DC3545",
            PresenceStatus.Away or PresenceStatus.BeRightBack => "#FFC107",
            PresenceStatus.Offline => "#6C757D",
            PresenceStatus.OutOfOffice => "#6C757D",
            _ => "#ADB5BD"
        };
    }

    #endregion
}

