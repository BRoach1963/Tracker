using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for Google People API operations - profile photos and contact lookup.
/// </summary>
public class GooglePeopleService
{
    #region Singleton

    private static readonly Lazy<GooglePeopleService> _instance =
        new(() => new GooglePeopleService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static GooglePeopleService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "google_people.log");

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        catch { /* Logging should never throw */ }
    }

    #endregion

    #region Fields

    private readonly Dictionary<string, CachedPhoto> _photoCache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);
    private readonly HttpClient _httpClient = new();

    #endregion

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private GooglePeopleService() { }

    #region Profile Photo Methods

    /// <summary>
    /// Gets the current authenticated user's profile photo URL.
    /// </summary>
    public async Task<string?> GetMyPhotoUrlAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;

        var service = GoogleAuthService.Instance.GetPeopleService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return null;
        }

        try
        {
            var request = service.People.Get("people/me");
            request.PersonFields = "photos";

            var person = await request.ExecuteAsync(cancellationToken);
            var photoUrl = person.Photos?.FirstOrDefault()?.Url;

            Log($"Got photo URL for current user");
            return photoUrl;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Failed to get user photo: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a contact's profile photo URL by email address.
    /// Returns cached URL if available and not expired.
    /// </summary>
    public async Task<string?> GetPhotoUrlByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(email)) return null;

        var emailKey = email.ToLowerInvariant();

        // Check cache first
        if (_photoCache.TryGetValue(emailKey, out var cached))
        {
            if (DateTime.UtcNow - cached.Timestamp < _cacheExpiry)
            {
                return cached.Url;
            }
            _photoCache.Remove(emailKey);
        }

        var service = GoogleAuthService.Instance.GetPeopleService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return null;
        }

        try
        {
            // Search contacts for this email
            var request = service.People.SearchContacts();
            request.Query = email;
            request.ReadMask = "photos,emailAddresses";
            request.PageSize = 10;

            var response = await request.ExecuteAsync(cancellationToken);
            var person = response.Results?.FirstOrDefault(r =>
                r.Person?.EmailAddresses?.Any(e =>
                    e.Value?.Equals(email, StringComparison.OrdinalIgnoreCase) == true) == true)?.Person;

            var photoUrl = person?.Photos?.FirstOrDefault()?.Url;

            // Cache the result (even if null, to avoid repeated lookups)
            _photoCache[emailKey] = new CachedPhoto
            {
                Url = photoUrl,
                Timestamp = DateTime.UtcNow
            };

            if (photoUrl != null)
            {
                Log($"Found photo for {email}");
            }

            return photoUrl;
        }
        catch (Exception ex)
        {
            Log($"No photo found for {email}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads a profile photo as byte array for display.
    /// </summary>
    public async Task<byte[]?> DownloadPhotoAsync(
        string photoUrl, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(photoUrl)) return null;

        try
        {
            var response = await _httpClient.GetAsync(photoUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log($"Failed to download photo: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets photo URLs for multiple email addresses in batch.
    /// Uses caching to minimize API calls.
    /// </summary>
    public async Task<Dictionary<string, string?>> GetPhotoUrlsForEmailsAsync(
        IEnumerable<string> emails,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string?>();

        foreach (var email in emails.Distinct())
        {
            if (string.IsNullOrEmpty(email)) continue;

            try
            {
                var photoUrl = await GetPhotoUrlByEmailAsync(email, cancellationToken);
                results[email] = photoUrl;
            }
            catch
            {
                results[email] = null;
            }
        }

        return results;
    }

    #endregion

    #region Contact Search Methods

    /// <summary>
    /// Searches the user's contacts.
    /// </summary>
    public async Task<List<ContactInfo>> SearchContactsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ContactInfo>();

        var service = GoogleAuthService.Instance.GetPeopleService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return results;
        }

        try
        {
            var request = service.People.SearchContacts();
            request.Query = query;
            request.ReadMask = "names,emailAddresses,photos,organizations";
            request.PageSize = maxResults;

            var response = await request.ExecuteAsync(cancellationToken);

            if (response.Results != null)
            {
                foreach (var result in response.Results)
                {
                    var person = result.Person;
                    if (person == null) continue;

                    var contact = new ContactInfo
                    {
                        ResourceName = person.ResourceName,
                        DisplayName = person.Names?.FirstOrDefault()?.DisplayName,
                        Email = person.EmailAddresses?.FirstOrDefault()?.Value,
                        PhotoUrl = person.Photos?.FirstOrDefault()?.Url,
                        Organization = person.Organizations?.FirstOrDefault()?.Name,
                        Title = person.Organizations?.FirstOrDefault()?.Title
                    };

                    results.Add(contact);
                }
            }

            Log($"Found {results.Count} contacts for query: {query}");
            return results;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Contact search failed: {ex.Message}");
            return results;
        }
    }

    /// <summary>
    /// Searches the organization directory (Google Workspace only).
    /// Returns empty list for personal Google accounts.
    /// </summary>
    public async Task<List<ContactInfo>> SearchDirectoryAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ContactInfo>();

        var service = GoogleAuthService.Instance.GetPeopleService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return results;
        }

        try
        {
            var request = service.People.SearchDirectoryPeople();
            request.Query = query;
            request.ReadMask = "names,emailAddresses,photos,organizations";
            request.PageSize = maxResults;
            request.Sources = PeopleResource.SearchDirectoryPeopleRequest.SourcesEnum.DIRECTORYSOURCETYPEDOMAINPROFILE;

            var response = await request.ExecuteAsync(cancellationToken);

            if (response.People != null)
            {
                foreach (var person in response.People)
                {
                    var contact = new ContactInfo
                    {
                        ResourceName = person.ResourceName,
                        DisplayName = person.Names?.FirstOrDefault()?.DisplayName,
                        Email = person.EmailAddresses?.FirstOrDefault()?.Value,
                        PhotoUrl = person.Photos?.FirstOrDefault()?.Url,
                        Organization = person.Organizations?.FirstOrDefault()?.Name,
                        Title = person.Organizations?.FirstOrDefault()?.Title
                    };

                    results.Add(contact);
                }
            }

            Log($"Found {results.Count} directory results for: {query}");
            return results;
        }
        catch (Exception ex)
        {
            // Directory search may not be available for personal accounts
            Log($"Directory search not available: {ex.Message}");
            return results;
        }
    }

    /// <summary>
    /// Gets a contact by resource name.
    /// </summary>
    public async Task<ContactInfo?> GetContactAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        var service = GoogleAuthService.Instance.GetPeopleService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return null;
        }

        try
        {
            var request = service.People.Get(resourceName);
            request.PersonFields = "names,emailAddresses,photos,organizations,phoneNumbers";

            var person = await request.ExecuteAsync(cancellationToken);

            return new ContactInfo
            {
                ResourceName = person.ResourceName,
                DisplayName = person.Names?.FirstOrDefault()?.DisplayName,
                Email = person.EmailAddresses?.FirstOrDefault()?.Value,
                PhotoUrl = person.Photos?.FirstOrDefault()?.Url,
                Organization = person.Organizations?.FirstOrDefault()?.Name,
                Title = person.Organizations?.FirstOrDefault()?.Title,
                PhoneNumber = person.PhoneNumbers?.FirstOrDefault()?.Value
            };
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Failed to get contact {resourceName}: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Clears the photo cache.
    /// </summary>
    public void ClearCache()
    {
        _photoCache.Clear();
        Log("Photo cache cleared");
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    public void PruneCache()
    {
        var expiredKeys = _photoCache
            .Where(kvp => DateTime.UtcNow - kvp.Value.Timestamp >= _cacheExpiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _photoCache.Remove(key);
        }

        if (expiredKeys.Any())
        {
            Log($"Pruned {expiredKeys.Count} expired cache entries");
        }
    }

    #endregion

    #region Internal Types

    private class CachedPhoto
    {
        public string? Url { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}

/// <summary>
/// Represents a contact from Google People API.
/// </summary>
public class ContactInfo
{
    public string? ResourceName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Organization { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Display name with fallback to email.
    /// </summary>
    public string DisplayNameOrEmail => DisplayName ?? Email ?? "Unknown";

    /// <summary>
    /// Initials for avatar placeholder.
    /// </summary>
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(DisplayName))
            {
                return Email?.Length > 0 ? Email[0].ToString().ToUpper() : "?";
            }

            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : DisplayName.Length > 0 ? DisplayName[0].ToString().ToUpper() : "?";
        }
    }
}
