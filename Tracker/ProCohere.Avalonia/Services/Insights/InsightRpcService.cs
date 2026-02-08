using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Implements RPC calls to Supabase for insight operations.
/// All insight/action creation goes through RPCs for security.
/// </summary>
public class InsightRpcService : IInsightRpcService
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;
    
    public async Task<int> CreateInsightsBatchAsync(Guid? generatedFor, IReadOnlyList<Insight> insights)
    {
        if (insights.Count == 0)
            return 0;
            
        var items = insights.Select(MapToRpcPayload).ToList();
        
        Debug.WriteLine($"[InsightRpcService] Creating batch of {items.Count} insights");
        
        var result = await Client.Rpc("ai_insight_create_batch", new
        {
            p_generated_for = generatedFor,
            p_items = JsonSerializer.Serialize(items)
        });
        
        if (result.Content == null)
        {
            Debug.WriteLine("[InsightRpcService] Batch create returned null");
            return 0;
        }
        
        var count = int.Parse(result.Content);
        Debug.WriteLine($"[InsightRpcService] Batch created {count} insights");
        return count;
    }
    
    public async Task<Guid> CreateInsightAsync(Guid? generatedFor, Insight insight)
    {
        Debug.WriteLine($"[InsightRpcService] Creating single insight: {insight.Type}");
        
        var result = await Client.Rpc("ai_insight_create", new
        {
            p_generated_for = generatedFor,
            p_insight_type = insight.Type.ToString().ToSnakeCase(),
            p_title = insight.Title,
            p_content = insight.Content,
            p_signature_hash = insight.SignatureHash,
            p_rule_key = insight.RuleKey,
            p_severity = insight.SeverityLevel,
            p_relevance_score = insight.RelevanceScore,
            p_subject_type = insight.SubjectType,
            p_subject_id = insight.SubjectId,
            p_source_type = insight.SourceType,
            p_source_id = insight.SourceId,
            p_expires_at = insight.ExpiresAt
        });
        
        if (result.Content == null)
            throw new InvalidOperationException("Failed to create insight via RPC");
            
        return Guid.Parse(result.Content.Trim('"'));
    }
    
    public async Task<Guid> CreateActionAsync(
        string signatureHash, 
        string actionType, 
        DateTime? expiresAt = null, 
        Guid? insightId = null, 
        string? reason = null)
    {
        if (!InsightSignature.IsValid(signatureHash))
            throw new ArgumentException("Invalid signature hash", nameof(signatureHash));
            
        Debug.WriteLine($"[InsightRpcService] Creating action: {actionType} for {signatureHash[..8]}...");
        
        var result = await Client.Rpc("ai_insight_action_create", new
        {
            p_signature_hash = signatureHash,
            p_action_type = actionType,
            p_expires_at = expiresAt,
            p_insight_id = insightId,
            p_action_reason = reason,
            p_action_metadata = "{}"
        });
        
        if (result.Content == null)
            throw new InvalidOperationException("Failed to create action via RPC");
            
        return Guid.Parse(result.Content.Trim('"'));
    }
    
    private static object MapToRpcPayload(Insight insight)
    {
        return new
        {
            insight_type = insight.Type.ToString().ToSnakeCase(),
            title = insight.Title,
            content = insight.Content,
            signature_hash = insight.SignatureHash,
            rule_key = insight.RuleKey,
            severity = insight.SeverityLevel,
            relevance_score = insight.RelevanceScore,
            subject_type = insight.SubjectType,
            subject_id = insight.SubjectId,
            source_type = insight.SourceType,
            source_id = insight.SourceId,
            expires_at = insight.ExpiresAt?.ToString("o")
        };
    }
}

/// <summary>
/// String extension for snake_case conversion.
/// </summary>
internal static class StringExtensions
{
    public static string ToSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(text[0]));
        
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(text[i]));
            }
            else
            {
                result.Append(text[i]);
            }
        }
        
        return result.ToString();
    }
}
