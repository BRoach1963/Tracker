namespace ProCohere.Avalonia.Models;

/// <summary>
/// Status of a background refresh operation.
/// Used for non-blocking refresh indicators on surfaces.
/// </summary>
public enum RefreshStatus
{
    /// <summary>No refresh in progress, status chip hidden.</summary>
    Idle,
    
    /// <summary>Refresh is in progress. Only shown after 400ms delay to avoid flicker.</summary>
    Updating,
    
    /// <summary>Refresh completed successfully. Fades out after a short delay.</summary>
    Updated
}
