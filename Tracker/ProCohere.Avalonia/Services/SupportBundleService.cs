using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for creating and submitting support bundles (logs + diagnostics).
/// </summary>
public class SupportBundleService
{
    #region Singleton

    private static readonly Lazy<SupportBundleService> _instance =
        new(() => new SupportBundleService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static SupportBundleService Instance => _instance.Value;

    #endregion

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere");

    private static readonly string TempDirectory = Path.Combine(
        Path.GetTempPath(), "ProCohere");

    public string? LastError { get; private set; }

    private SupportBundleService() 
    {
        Directory.CreateDirectory(TempDirectory);
    }

    /// <summary>
    /// Creates a zip bundle containing all log files and system info.
    /// </summary>
    public async Task<string?> CreateBundleAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;

        try
        {
            var bundleName = $"support_bundle_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var bundlePath = Path.Combine(TempDirectory, bundleName);

            using (var zipArchive = ZipFile.Open(bundlePath, ZipArchiveMode.Create))
            {
                // Add all log files
                if (Directory.Exists(LogDirectory))
                {
                    var logFiles = Directory.GetFiles(LogDirectory, "*.log");
                    foreach (var logFile in logFiles)
                    {
                        var fileName = Path.GetFileName(logFile);
                        try
                        {
                            // Read with sharing to avoid locking issues
                            using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            var entry = zipArchive.CreateEntry($"logs/{fileName}");
                            using var entryStream = entry.Open();
                            await stream.CopyToAsync(entryStream, cancellationToken);
                        }
                        catch
                        {
                            // Skip files that can't be read
                        }
                    }
                }

                // Add system info
                var systemInfo = GetSystemInfo();
                var systemInfoEntry = zipArchive.CreateEntry("system_info.txt");
                using (var writer = new StreamWriter(systemInfoEntry.Open()))
                {
                    await writer.WriteAsync(systemInfo);
                }
            }

            return bundlePath;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Uploads bundle to Supabase Storage and returns a signed URL.
    /// </summary>
    public async Task<string?> UploadBundleAsync(
        string bundlePath, 
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not connected to Supabase";
                return null;
            }

            var fileName = Path.GetFileName(bundlePath);
            var storagePath = $"bundles/{DateTime.UtcNow:yyyy/MM}/{fileName}";

            var fileBytes = await File.ReadAllBytesAsync(bundlePath, cancellationToken);

            // Upload to storage bucket
            var result = await client.Storage
                .From("support-bundles")
                .Upload(fileBytes, storagePath, new Supabase.Storage.FileOptions
                {
                    ContentType = "application/zip"
                });

            if (string.IsNullOrEmpty(result))
            {
                LastError = "Upload failed";
                return null;
            }

            // Get signed URL (valid for 7 days)
            var signedUrl = await client.Storage
                .From("support-bundles")
                .CreateSignedUrl(storagePath, 60 * 60 * 24 * 7); // 7 days in seconds

            return signedUrl;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Sends the support request to the Edge Function which emails support.
    /// </summary>
    public async Task<bool> SendSupportRequestAsync(
        string subject,
        string description,
        string? bundleUrl = null,
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        try
        {
            // Get current user info
            var userEmail = AuthService.Instance.CurrentUser?.Email ?? "unknown@user.com";
            var userName = AuthService.Instance.CurrentTeamMember?.FullName ?? "Unknown User";
            var orgId = AuthService.Instance.CurrentTeamMember?.OrganizationId;

            var payload = new
            {
                user_email = userEmail,
                user_name = userName,
                organization_id = orgId?.ToString(),
                subject = subject,
                description = description,
                bundle_url = bundleUrl,
                app_version = GetAppVersion(),
                os_info = GetOsInfo()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call Edge Function
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseConfig.AnonKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseConfig.AnonKey}");

            var response = await httpClient.PostAsync(
                $"{SupabaseConfig.ProjectUrl}/functions/v1/send-support-bundle",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                LastError = $"Failed to send: {error}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Full workflow: Create bundle, upload, and send support request.
    /// </summary>
    public async Task<bool> SubmitSupportRequestAsync(
        string subject,
        string description,
        bool includeLogs = true,
        CancellationToken cancellationToken = default)
    {
        string? bundleUrl = null;

        if (includeLogs)
        {
            // Create bundle
            var bundlePath = await CreateBundleAsync(cancellationToken);
            if (bundlePath != null)
            {
                // Upload bundle
                bundleUrl = await UploadBundleAsync(bundlePath, cancellationToken);
                
                // Clean up temp file
                try { File.Delete(bundlePath); } catch { }
            }
        }

        // Send request
        return await SendSupportRequestAsync(subject, description, bundleUrl, cancellationToken);
    }

    #region Private Methods

    private static string GetSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ProCohere System Information ===");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("--- Application ---");
        sb.AppendLine($"Version: {GetAppVersion()}");
        sb.AppendLine();
        sb.AppendLine("--- Operating System ---");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
        sb.AppendLine($"Machine Name: {Environment.MachineName}");
        sb.AppendLine();
        sb.AppendLine("--- Runtime ---");
        sb.AppendLine($".NET Version: {Environment.Version}");
        sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine();
        sb.AppendLine("--- Memory ---");
        sb.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
        sb.AppendLine();
        sb.AppendLine("--- User ---");
        sb.AppendLine($"User: {AuthService.Instance.CurrentUser?.Email ?? "Not logged in"}");
        sb.AppendLine($"Team Member: {AuthService.Instance.CurrentTeamMember?.FullName ?? "None"}");
        sb.AppendLine($"Organization ID: {AuthService.Instance.CurrentTeamMember?.OrganizationId.ToString() ?? "None"}");
        
        return sb.ToString();
    }

    private static string GetAppVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static string GetOsInfo()
    {
        return $"{Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";
    }

    #endregion
}
