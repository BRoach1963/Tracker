using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// App installation model - tracks device activations.
    /// </summary>
    [Table("app_installations")]
    public class AppInstallation : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Column("device_name")]
        public string? DeviceName { get; set; }

        [Column("os_version")]
        public string? OsVersion { get; set; }

        [Column("app_version")]
        public string? AppVersion { get; set; }

        [Column("activated_at")]
        public DateTime ActivatedAt { get; set; }

        [Column("last_seen_at")]
        public DateTime LastSeenAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}

