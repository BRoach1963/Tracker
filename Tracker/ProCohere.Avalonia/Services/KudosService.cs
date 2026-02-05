using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using Supabase;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing kudos/recognition between team members.
/// Handles CRUD operations for in-app recognition (no external delivery).
/// </summary>
public class KudosService
{
    #region Singleton

    private static readonly Lazy<KudosService> _instance =
        new(() => new KudosService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static KudosService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "kudos.log");

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

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private KudosService() { }

    #region Create

    /// <summary>
    /// Create new kudos recognition.
    /// </summary>
    public async Task<Kudos?> CreateKudosAsync(Guid fromMemberId, Guid toMemberId, string message, string? category = null, bool isPublic = true)
    {
        LastError = null;

        try
        {
            if (fromMemberId == Guid.Empty)
            {
                LastError = "Invalid sender";
                Log(LastError);
                return null;
            }

            if (toMemberId == Guid.Empty)
            {
                LastError = "Invalid recipient";
                Log(LastError);
                return null;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                LastError = "Message is required";
                Log(LastError);
                return null;
            }

            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                Log(LastError);
                return null;
            }

            var currentUser = AuthService.Instance.CurrentTeamMember;
            if (currentUser == null)
            {
                LastError = "Current user not found";
                Log(LastError);
                return null;
            }

            var kudos = new Kudos
            {
                Id = Guid.NewGuid(),
                OrganizationId = currentUser.OrganizationId,
                FromMemberId = fromMemberId,
                ToMemberId = toMemberId,
                Message = message.Trim(),
                Category = category,
                IsPublic = isPublic,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await client.From<Kudos>()
                .Insert(kudos);

            if (response?.Model != null)
            {
                Log($"Created kudos: {kudos.Id} from {fromMemberId} to {toMemberId}");
                return response.Model;
            }

            LastError = "Failed to create kudos";
            Log(LastError);
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"Create failed: {ex.Message}";
            Log($"CreateKudosAsync error: {ex}");
            return null;
        }
    }

    #endregion

    #region Read

    /// <summary>
    /// Get all kudos received by a team member.
    /// </summary>
    public async Task<List<Kudos>> GetKudosReceivedAsync(Guid teamMemberId)
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                Log("GetKudosReceivedAsync: Not authenticated");
                return new List<Kudos>();
            }

            var response = await client.From<Kudos>()
                .Where(k => k.ToMemberId == teamMemberId && k.IsDeleted == false)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return response?.Models ?? new List<Kudos>();
        }
        catch (Exception ex)
        {
            Log($"GetKudosReceivedAsync error: {ex}");
            return new List<Kudos>();
        }
    }

    /// <summary>
    /// Get all kudos sent by a team member.
    /// </summary>
    public async Task<List<Kudos>> GetKudosSentAsync(Guid teamMemberId)
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                Log("GetKudosSentAsync: Not authenticated");
                return new List<Kudos>();
            }

            var response = await client.From<Kudos>()
                .Where(k => k.FromMemberId == teamMemberId && k.IsDeleted == false)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return response?.Models ?? new List<Kudos>();
        }
        catch (Exception ex)
        {
            Log($"GetKudosSentAsync error: {ex}");
            return new List<Kudos>();
        }
    }

    /// <summary>
    /// Get all public kudos for the organization.
    /// </summary>
    public async Task<List<Kudos>> GetPublicKudosAsync()
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                Log("GetPublicKudosAsync: Not authenticated");
                return new List<Kudos>();
            }

            var currentUser = AuthService.Instance.CurrentTeamMember;
            if (currentUser == null)
            {
                Log("GetPublicKudosAsync: Current user not found");
                return new List<Kudos>();
            }

            var response = await client.From<Kudos>()
                .Where(k => k.OrganizationId == currentUser.OrganizationId && k.IsPublic == true && k.IsDeleted == false)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(50) // Limit to recent 50
                .Get();

            return response?.Models ?? new List<Kudos>();
        }
        catch (Exception ex)
        {
            Log($"GetPublicKudosAsync error: {ex}");
            return new List<Kudos>();
        }
    }

    /// <summary>
    /// Get kudos by ID.
    /// </summary>
    public async Task<Kudos?> GetKudosByIdAsync(Guid kudosId)
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                Log("GetKudosByIdAsync: Not authenticated");
                return null;
            }

            var response = await client.From<Kudos>()
                .Where(k => k.Id == kudosId && k.IsDeleted == false)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            Log($"GetKudosByIdAsync error: {ex}");
            return null;
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Update kudos message (only sender can edit).
    /// </summary>
    public async Task<bool> UpdateKudosAsync(Kudos kudos)
    {
        LastError = null;

        try
        {
            if (kudos == null || kudos.Id == Guid.Empty)
            {
                LastError = "Invalid kudos";
                Log(LastError);
                return false;
            }

            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                Log(LastError);
                return false;
            }

            kudos.UpdatedAt = DateTime.UtcNow;

            await client.From<Kudos>()
                .Where(k => k.Id == kudos.Id)
                .Update(kudos);

            Log($"Updated kudos: {kudos.Id}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Update failed: {ex.Message}";
            Log($"UpdateKudosAsync error: {ex}");
            return false;
        }
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft delete kudos.
    /// </summary>
    public async Task<bool> DeleteKudosAsync(Guid kudosId)
    {
        LastError = null;

        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                Log(LastError);
                return false;
            }

            var currentUser = AuthService.Instance.CurrentUser;
            if (currentUser == null)
            {
                LastError = "Current user not found";
                Log(LastError);
                return false;
            }

            // Soft delete
            var kudos = await GetKudosByIdAsync(kudosId);
            if (kudos == null)
            {
                LastError = "Kudos not found";
                Log(LastError);
                return false;
            }

            kudos.IsDeleted = true;
            kudos.DeletedAt = DateTime.UtcNow;
            kudos.DeletedBy = string.IsNullOrEmpty(currentUser.Id) ? Guid.Empty : Guid.Parse(currentUser.Id);
            kudos.UpdatedAt = DateTime.UtcNow;

            await client.From<Kudos>()
                .Where(k => k.Id == kudosId)
                .Update(kudos);

            Log($"Deleted kudos: {kudosId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Delete failed: {ex.Message}";
            Log($"DeleteKudosAsync error: {ex}");
            return false;
        }
    }

    #endregion
}
