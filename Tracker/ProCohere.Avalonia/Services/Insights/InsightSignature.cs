using System;
using System.Security.Cryptography;
using System.Text;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Generates stable signature hashes for insights.
/// Used for deduplication and matching user actions to insights.
/// </summary>
public static class InsightSignature
{
    /// <summary>
    /// Generates a 64-character hex SHA-256 signature hash for an insight.
    /// </summary>
    /// <param name="type">The insight type.</param>
    /// <param name="subjectType">The subject entity type (e.g., "team_member", "goal").</param>
    /// <param name="subjectId">The subject entity ID.</param>
    /// <param name="ruleKey">The analyzer rule key (e.g., "overdue_1to1_critical").</param>
    /// <returns>64-character lowercase hex string.</returns>
    public static string Generate(
        Models.InsightType type, 
        string subjectType, 
        Guid subjectId, 
        string ruleKey)
    {
        // Format: type|subject_type|subject_id|rule_key
        var input = $"{type}|{subjectType}|{subjectId}|{ruleKey}";
        return ComputeHash(input);
    }
    
    /// <summary>
    /// Generates a signature hash from string components.
    /// </summary>
    public static string Generate(
        string insightType, 
        string subjectType, 
        Guid subjectId, 
        string ruleKey)
    {
        var input = $"{insightType}|{subjectType}|{subjectId}|{ruleKey}";
        return ComputeHash(input);
    }
    
    /// <summary>
    /// Generates a signature hash for insights without a specific subject.
    /// Uses the source entity as the subject.
    /// </summary>
    public static string GenerateFromSource(
        Models.InsightType type,
        string sourceType,
        Guid sourceId,
        string ruleKey)
    {
        var input = $"{type}|{sourceType}|{sourceId}|{ruleKey}";
        return ComputeHash(input);
    }
    
    /// <summary>
    /// Validates that a signature hash is the correct format (64 hex chars).
    /// </summary>
    public static bool IsValid(string? signatureHash)
    {
        if (string.IsNullOrEmpty(signatureHash))
            return false;
            
        if (signatureHash.Length != 64)
            return false;
            
        foreach (var c in signatureHash)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        
        return true;
    }
    
    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
