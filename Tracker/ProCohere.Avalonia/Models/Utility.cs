using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Tag model - maps to the tags table in Supabase procohere schema.
/// Organization-level tags for categorizing various entities.
/// </summary>
[Table("tags")]
public class Tag : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Color hex code for display (e.g., '#FF5733').
    /// </summary>
    [Column("color")]
    public string? Color { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion
}

/// <summary>
/// Attachment model - maps to the attachments table in Supabase procohere schema.
/// File attachments linked to various entities (meetings, tasks, notes, etc.).
/// </summary>
[Table("attachments")]
public class Attachment : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("uploaded_by")]
    public Guid UploadedBy { get; set; }

    #endregion

    #region Entity Link

    /// <summary>
    /// Type of entity: 'meeting', 'task', 'note', 'goal', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the linked entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    #endregion

    #region File Info

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [Column("file_size")]
    public long? FileSize { get; set; }

    /// <summary>
    /// MIME type (e.g., 'application/pdf', 'image/png').
    /// </summary>
    [Column("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// Path in Supabase Storage bucket.
    /// </summary>
    [Column("storage_path")]
    public string StoragePath { get; set; } = string.Empty;

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Human-readable file size.
    /// </summary>
    public string FileSizeDisplay
    {
        get
        {
            if (!FileSize.HasValue) return "Unknown";
            var size = FileSize.Value;
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
            if (size < 1024 * 1024 * 1024) return $"{size / (1024.0 * 1024):F1} MB";
            return $"{size / (1024.0 * 1024 * 1024):F1} GB";
        }
    }

    /// <summary>
    /// File extension from filename.
    /// </summary>
    public string FileExtension => System.IO.Path.GetExtension(FileName)?.TrimStart('.').ToUpperInvariant() ?? "";

    /// <summary>
    /// Whether this is an image file.
    /// </summary>
    public bool IsImage => MimeType?.StartsWith("image/") == true;

    #endregion
}
