using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Logging;

namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Service for interacting with Microsoft Graph API.
    /// Handles calendar operations, user profile, and Teams (when available).
    /// </summary>
    public class MicrosoftGraphService : IDisposable
    {
        #region Singleton

        private static MicrosoftGraphService? _instance;
        private static readonly object _lock = new();

        public static MicrosoftGraphService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new MicrosoftGraphService();
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

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        #endregion

        #region Properties

        /// <summary>
        /// Whether Microsoft 365 integration is connected and ready.
        /// </summary>
        public bool IsConnected => MicrosoftGraphAuthService.Instance.IsAuthenticated;

        /// <summary>
        /// Whether Calendar sync is available.
        /// </summary>
        public bool CalendarAvailable => MicrosoftGraphAuthService.Instance.CalendarAvailable;

        /// <summary>
        /// Whether Teams features are available.
        /// </summary>
        public bool TeamsAvailable => MicrosoftGraphAuthService.Instance.TeamsAvailable;

        #endregion

        #region Constructor

        private MicrosoftGraphService()
        {
            _logger = LoggingManager.GetComponentLogger("GraphService");
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(GraphBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #endregion

        #region User Profile

        /// <summary>
        /// Gets the current user's profile information.
        /// </summary>
        public async Task<GraphUser?> GetCurrentUserAsync()
        {
            return await ExecuteGetAsync<GraphUser>("/me");
        }

        #endregion

        #region Calendar Operations

        /// <summary>
        /// Gets calendar events within a date range.
        /// </summary>
        /// <param name="startDate">Start of date range.</param>
        /// <param name="endDate">End of date range.</param>
        /// <returns>List of calendar events.</returns>
        public async Task<List<GraphCalendarEvent>> GetCalendarEventsAsync(
            DateTime startDate, DateTime endDate)
        {
            var start = startDate.ToUniversalTime().ToString("o");
            var end = endDate.ToUniversalTime().ToString("o");
            
            var url = $"/me/calendarView?startDateTime={start}&endDateTime={end}" +
                      "&$select=id,subject,start,end,location,bodyPreview,attendees,isAllDay,isCancelled" +
                      "&$orderby=start/dateTime" +
                      "&$top=100";

            var result = await ExecuteGetAsync<GraphListResponse<GraphCalendarEvent>>(url);
            return result?.Value ?? new List<GraphCalendarEvent>();
        }

        /// <summary>
        /// Gets calendar events using delta query (only changes since last sync).
        /// </summary>
        /// <param name="deltaLink">Delta link from previous sync, or null for initial sync.</param>
        /// <param name="startDate">Start date (only used for initial sync).</param>
        /// <param name="endDate">End date (only used for initial sync).</param>
        /// <returns>Changed events and new delta link.</returns>
        public async Task<(List<GraphCalendarEvent> Events, List<string> DeletedIds, string? NextDeltaLink)> 
            GetCalendarDeltaAsync(string? deltaLink, DateTime startDate, DateTime endDate)
        {
            string url;
            
            if (string.IsNullOrEmpty(deltaLink))
            {
                // Initial sync - get all events in range
                var start = startDate.ToUniversalTime().ToString("o");
                var end = endDate.ToUniversalTime().ToString("o");
                url = $"/me/calendarView/delta?startDateTime={start}&endDateTime={end}";
            }
            else
            {
                // Subsequent sync - use delta link
                url = deltaLink;
            }

            var events = new List<GraphCalendarEvent>();
            var deletedIds = new List<string>();
            string? nextDeltaLink = null;

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return (events, deletedIds, null);

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);

                // Follow pagination
                var currentUrl = url.StartsWith("http") ? url : $"{GraphBaseUrl}{url}";
                
                while (!string.IsNullOrEmpty(currentUrl))
                {
                    var response = await _httpClient.GetAsync(currentUrl);
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Error($"Delta query failed: {response.StatusCode} - {content}");
                        break;
                    }

                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    // Extract events
                    if (root.TryGetProperty("value", out var valueArray))
                    {
                        foreach (var item in valueArray.EnumerateArray())
                        {
                            // Check if this is a deleted item
                            if (item.TryGetProperty("@removed", out _))
                            {
                                if (item.TryGetProperty("id", out var idProp))
                                    deletedIds.Add(idProp.GetString()!);
                            }
                            else
                            {
                                var evt = JsonSerializer.Deserialize<GraphCalendarEvent>(
                                    item.GetRawText(), _jsonOptions);
                                if (evt != null)
                                    events.Add(evt);
                            }
                        }
                    }

                    // Check for next page or delta link
                    if (root.TryGetProperty("@odata.nextLink", out var nextLink))
                    {
                        currentUrl = nextLink.GetString();
                    }
                    else if (root.TryGetProperty("@odata.deltaLink", out var delta))
                    {
                        nextDeltaLink = delta.GetString();
                        currentUrl = null;
                    }
                    else
                    {
                        currentUrl = null;
                    }
                }

                _logger.Info($"Delta sync: {events.Count} events, {deletedIds.Count} deleted");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Delta query failed");
            }

            return (events, deletedIds, nextDeltaLink);
        }

        /// <summary>
        /// Creates a new calendar event.
        /// </summary>
        /// <param name="calendarEvent">Event details.</param>
        /// <returns>Created event with ID, or null if failed.</returns>
        public async Task<GraphCalendarEvent?> CreateCalendarEventAsync(GraphCalendarEvent calendarEvent)
        {
            return await ExecutePostAsync<GraphCalendarEvent>("/me/events", calendarEvent);
        }

        /// <summary>
        /// Updates an existing calendar event.
        /// </summary>
        /// <param name="eventId">Event ID from Microsoft Graph.</param>
        /// <param name="updates">Fields to update.</param>
        /// <returns>Updated event, or null if failed.</returns>
        public async Task<GraphCalendarEvent?> UpdateCalendarEventAsync(
            string eventId, GraphCalendarEvent updates)
        {
            return await ExecutePatchAsync<GraphCalendarEvent>($"/me/events/{eventId}", updates);
        }

        /// <summary>
        /// Deletes a calendar event.
        /// </summary>
        /// <param name="eventId">Event ID from Microsoft Graph.</param>
        /// <returns>True if deleted successfully.</returns>
        public async Task<bool> DeleteCalendarEventAsync(string eventId)
        {
            return await ExecuteDeleteAsync($"/me/events/{eventId}");
        }

        /// <summary>
        /// Gets a specific calendar event by ID.
        /// </summary>
        /// <param name="eventId">Event ID from Microsoft Graph.</param>
        /// <returns>Event details, or null if not found.</returns>
        public async Task<GraphCalendarEvent?> GetCalendarEventAsync(string eventId)
        {
            return await ExecuteGetAsync<GraphCalendarEvent>($"/me/events/{eventId}");
        }

        #endregion

        #region HTTP Methods

        private async Task<T?> ExecuteGetAsync<T>(string endpoint) where T : class
        {
            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.Warn("No access token available for Graph API call");
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                }

                _logger.Error($"Graph GET {endpoint} failed: {response.StatusCode} - {content}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Graph GET {endpoint} exception");
                return null;
            }
        }

        private async Task<T?> ExecutePostAsync<T>(string endpoint, object payload) where T : class
        {
            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                }

                _logger.Error($"Graph POST {endpoint} failed: {response.StatusCode} - {content}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Graph POST {endpoint} exception");
                return null;
            }
        }

        private async Task<T?> ExecutePatchAsync<T>(string endpoint, object payload) where T : class
        {
            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
                {
                    Content = httpContent
                };

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                }

                _logger.Error($"Graph PATCH {endpoint} failed: {response.StatusCode} - {content}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Graph PATCH {endpoint} exception");
                return null;
            }
        }

        private async Task<bool> ExecuteDeleteAsync(string endpoint)
        {
            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync(endpoint);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return true; // NotFound is ok for delete (already deleted)
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.Error($"Graph DELETE {endpoint} failed: {response.StatusCode} - {content}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Graph DELETE {endpoint} exception");
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        #endregion
    }

    #region Graph API Models

    /// <summary>
    /// Response wrapper for Microsoft Graph list endpoints.
    /// </summary>
    public class GraphListResponse<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }

        [JsonPropertyName("@odata.deltaLink")]
        public string? DeltaLink { get; set; }
    }

    /// <summary>
    /// Microsoft Graph user profile.
    /// </summary>
    public class GraphUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("mail")]
        public string? Mail { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("officeLocation")]
        public string? OfficeLocation { get; set; }
    }

    /// <summary>
    /// Microsoft Graph calendar event.
    /// </summary>
    public class GraphCalendarEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("bodyPreview")]
        public string? BodyPreview { get; set; }

        [JsonPropertyName("body")]
        public GraphItemBody? Body { get; set; }

        [JsonPropertyName("start")]
        public GraphDateTimeTimeZone? Start { get; set; }

        [JsonPropertyName("end")]
        public GraphDateTimeTimeZone? End { get; set; }

        [JsonPropertyName("location")]
        public GraphLocation? Location { get; set; }

        [JsonPropertyName("attendees")]
        public List<GraphAttendee>? Attendees { get; set; }

        [JsonPropertyName("isAllDay")]
        public bool IsAllDay { get; set; }

        [JsonPropertyName("isCancelled")]
        public bool IsCancelled { get; set; }

        [JsonPropertyName("isOnlineMeeting")]
        public bool IsOnlineMeeting { get; set; }

        [JsonPropertyName("onlineMeetingUrl")]
        public string? OnlineMeetingUrl { get; set; }

        [JsonPropertyName("changeKey")]
        public string? ChangeKey { get; set; }

        [JsonPropertyName("@odata.etag")]
        public string? ETag { get; set; }
    }

    /// <summary>
    /// Graph date/time with timezone.
    /// </summary>
    public class GraphDateTimeTimeZone
    {
        [JsonPropertyName("dateTime")]
        public string DateTime { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Converts to local DateTime.
        /// </summary>
        public System.DateTime ToLocalDateTime()
        {
            if (System.DateTime.TryParse(DateTime, out var dt))
            {
                // Graph returns in specified timezone, convert to local
                var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
                var utc = TimeZoneInfo.ConvertTimeToUtc(dt, tz);
                return utc.ToLocalTime();
            }
            return System.DateTime.MinValue;
        }

        /// <summary>
        /// Creates from local DateTime.
        /// </summary>
        public static GraphDateTimeTimeZone FromLocalDateTime(System.DateTime localDateTime)
        {
            return new GraphDateTimeTimeZone
            {
                DateTime = localDateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.0000000"),
                TimeZone = "UTC"
            };
        }
    }

    /// <summary>
    /// Graph item body (HTML or text).
    /// </summary>
    public class GraphItemBody
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "text";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Graph location.
    /// </summary>
    public class GraphLocation
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("address")]
        public GraphPhysicalAddress? Address { get; set; }
    }

    /// <summary>
    /// Graph physical address.
    /// </summary>
    public class GraphPhysicalAddress
    {
        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("countryOrRegion")]
        public string? CountryOrRegion { get; set; }
    }

    /// <summary>
    /// Graph attendee.
    /// </summary>
    public class GraphAttendee
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "required";

        [JsonPropertyName("status")]
        public GraphResponseStatus? Status { get; set; }

        [JsonPropertyName("emailAddress")]
        public GraphEmailAddress? EmailAddress { get; set; }
    }

    /// <summary>
    /// Graph response status.
    /// </summary>
    public class GraphResponseStatus
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }
    }

    /// <summary>
    /// Graph email address.
    /// </summary>
    public class GraphEmailAddress
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }

    #endregion
}

