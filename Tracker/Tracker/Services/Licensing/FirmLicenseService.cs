using Supabase;
using Tracker.Logging;
using Tracker.Services.Backend;

namespace Tracker.Services.Licensing
{
    /// <summary>
    /// Result of a firm seat validation check.
    /// </summary>
    public class SeatValidationResult
    {
        public bool IsValid { get; set; }
        public Guid? FirmId { get; set; }
        public string? FirmName { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string? Tier { get; set; }
        public string? SeatRole { get; set; }
        public string? ErrorMessage { get; set; }

        public static SeatValidationResult Valid(Guid firmId, string firmName, Guid subscriptionId, string tier, string role) => new()
        {
            IsValid = true,
            FirmId = firmId,
            FirmName = firmName,
            SubscriptionId = subscriptionId,
            Tier = tier,
            SeatRole = role
        };

        public static SeatValidationResult Invalid(string error) => new()
        {
            IsValid = false,
            ErrorMessage = error
        };
    }

    /// <summary>
    /// Service for validating firm licenses and seats via Supabase.
    /// </summary>
    public interface IFirmLicenseService
    {
        /// <summary>
        /// Check if an email has a valid seat for a product.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="product">Product name (tracker, procausa, praxis).</param>
        /// <returns>Validation result with firm info if valid.</returns>
        Task<SeatValidationResult> ValidateSeatAsync(string email, string product = "tracker");
    }

    /// <summary>
    /// Implementation of firm license service using Supabase.
    /// </summary>
    public class FirmLicenseService : IFirmLicenseService
    {
        private readonly ILogger _logger;
        private Supabase.Client? _client;
        private bool _isInitialized;

        public FirmLicenseService()
        {
            _logger = LoggingManager.GetComponentLogger("FirmLicense");
        }

        /// <summary>
        /// Initialize the Supabase client.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized && _client != null)
                return;

            try
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = false,
                    AutoConnectRealtime = false
                };

                _client = new Supabase.Client(SupabaseConfig.ProjectUrl, SupabaseConfig.AnonKey, options);
                await _client.InitializeAsync();
                _isInitialized = true;
                _logger.Info("FirmLicenseService initialized");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize FirmLicenseService");
                throw;
            }
        }

        /// <summary>
        /// Check if an email has a valid seat for a product.
        /// </summary>
        public async Task<SeatValidationResult> ValidateSeatAsync(string email, string product = "tracker")
        {
            try
            {
                await EnsureInitializedAsync();

                if (_client == null)
                {
                    return SeatValidationResult.Invalid("License service not available");
                }

                _logger.Info("Validating seat for {0} on {1}", email, product);

                // Call the check_seat_valid function we created in Supabase
                var response = await _client.Rpc("check_seat_valid", new
                {
                    p_email = email.ToLowerInvariant(),
                    p_product = product
                });

                if (response == null)
                {
                    _logger.Warn("No seat found for {0} on {1}", email, product);
                    return SeatValidationResult.Invalid("No valid license seat found for this email");
                }

                // Parse the response - it's a JSON array with one row (or empty)
                var content = response.Content;
                _logger.Debug("Seat validation response: {0}", content);

                if (string.IsNullOrEmpty(content) || content == "[]" || content == "null")
                {
                    _logger.Warn("No seat found for {0} on {1}", email, product);
                    return SeatValidationResult.Invalid("No valid license seat found for this email");
                }

                // Parse JSON response
                var results = System.Text.Json.JsonSerializer.Deserialize<List<SeatCheckResult>>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (results == null || results.Count == 0 || !results[0].IsValid)
                {
                    _logger.Warn("Seat not valid for {0} on {1}", email, product);
                    return SeatValidationResult.Invalid("License seat is not active");
                }

                var result = results[0];
                _logger.Info("Seat valid for {0}: Firm={1}, Tier={2}, Role={3}", 
                    email, result.FirmName, result.Tier, result.SeatRole);

                return SeatValidationResult.Valid(
                    result.FirmId,
                    result.FirmName ?? "Unknown",
                    result.SubscriptionId,
                    result.Tier ?? "unknown",
                    result.SeatRole ?? "user");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to validate seat for {0}", email);
                return SeatValidationResult.Invalid($"License validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal class to deserialize Supabase RPC response.
        /// </summary>
        private class SeatCheckResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("is_valid")]
            public bool IsValid { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("firm_id")]
            public Guid FirmId { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("firm_name")]
            public string? FirmName { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("subscription_id")]
            public Guid SubscriptionId { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("tier")]
            public string? Tier { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("seat_role")]
            public string? SeatRole { get; set; }
        }
    }
}
