using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// Represents an audit log entry for subscription changes.
    /// </summary>
    [Table("subscription_events")]
    public class SubscriptionEvent : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("subscription_id")]
        public string SubscriptionId { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("event_type")]
        public string EventType { get; set; } = string.Empty;

        [Column("event_data")]
        public string? EventData { get; set; }

        [Column("square_event_id")]
        public string? SquareEventId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


