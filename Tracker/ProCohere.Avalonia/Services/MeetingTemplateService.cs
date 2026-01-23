using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing meeting templates.
/// Provides CRUD operations and default template initialization.
/// 
/// Note: Template items are stored as JSONB in meeting_templates.default_agenda,
/// NOT as a separate table. Items are serialized/deserialized by this service.
/// </summary>
public class MeetingTemplateService
{
    #region Singleton

    private static readonly Lazy<MeetingTemplateService> _instance =
        new(() => new MeetingTemplateService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MeetingTemplateService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "meeting_template_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private MeetingTemplateService() { }

    #region Template CRUD

    /// <summary>
    /// Gets all templates visible to the current user (own + organization templates).
    /// </summary>
    public async Task<List<MeetingTemplateDetail>> GetTemplatesAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MeetingTemplateDetail>();
        }

        try
        {
            var profile = AuthService.Instance.CurrentProfile;
            if (profile == null)
            {
                LastError = "No profile";
                return new List<MeetingTemplateDetail>();
            }

            // Get templates (RLS filters by organization)
            var response = await client
                .From<MeetingTemplateDetail>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var templates = response.Models.ToList();
            Log($"Loaded {templates.Count} templates");

            // Parse items from JSONB for each template
            foreach (var template in templates)
            {
                ParseTemplateItems(template);
            }

            return templates;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error loading templates: {ex.Message}");
            return new List<MeetingTemplateDetail>();
        }
    }

    /// <summary>
    /// Gets templates by meeting type.
    /// </summary>
    public async Task<List<MeetingTemplateDetail>> GetTemplatesByMeetingTypeAsync(string meetingType)
    {
        var templates = await GetTemplatesAsync();
        return templates.Where(t => t.MeetingType == meetingType).ToList();
    }

    /// <summary>
    /// Gets a single template by ID with items loaded.
    /// </summary>
    public async Task<MeetingTemplateDetail?> GetTemplateAsync(Guid templateId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var response = await client
                .From<MeetingTemplateDetail>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, templateId.ToString())
                .Single();

            if (response != null)
            {
                ParseTemplateItems(response);
            }

            return response;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error loading template {templateId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses template items from the JSONB default_agenda column.
    /// </summary>
    private void ParseTemplateItems(MeetingTemplateDetail template)
    {
        template.Items = new List<MeetingTemplateItem>();
        
        if (string.IsNullOrEmpty(template.DefaultAgendaJson))
            return;

        try
        {
            var items = JsonSerializer.Deserialize<List<MeetingTemplateItem>>(
                template.DefaultAgendaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (items != null)
            {
                template.Items = items;
            }
        }
        catch (Exception ex)
        {
            Log($"Error parsing template items for {template.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// Serializes template items to JSONB format.
    /// </summary>
    private string SerializeTemplateItems(List<MeetingTemplateItem> items)
    {
        return JsonSerializer.Serialize(items, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Creates a new template.
    /// </summary>
    public async Task<MeetingTemplateDetail?> CreateTemplateAsync(
        string name,
        string? description,
        string meetingType,
        List<(string Title, string? Description, bool IsOptional, int? DurationMinutes)> items,
        bool isSystemTemplate = false)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var profile = AuthService.Instance.CurrentProfile;
            if (profile == null || !profile.OrganizationId.HasValue)
            {
                LastError = "No profile or organization";
                return null;
            }

            // Build the template items
            var templateItems = items.Select((item, index) => new MeetingTemplateItem
            {
                Id = Guid.NewGuid(),
                Title = item.Title,
                Description = item.Description,
                IsOptional = item.IsOptional,
                SuggestedDurationMinutes = item.DurationMinutes,
                SortOrder = index
            }).ToList();

            var template = new MeetingTemplateDetail
            {
                Id = Guid.NewGuid(),
                OrganizationId = profile.OrganizationId.Value,
                CreatedBy = profile.Id,
                Name = name,
                Description = description,
                MeetingType = meetingType,
                DefaultAgendaJson = SerializeTemplateItems(templateItems),
                IsSystemTemplate = isSystemTemplate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await client
                .From<MeetingTemplateDetail>()
                .Insert(template);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                LastError = "Failed to create template";
                return null;
            }

            Log($"Created template: {name} with {items.Count} items");
            
            // Return with parsed items
            ParseTemplateItems(created);
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error creating template: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates a template's items.
    /// </summary>
    public async Task<bool> UpdateTemplateItemsAsync(Guid templateId, List<MeetingTemplateItem> items)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var template = await GetTemplateAsync(templateId);
            if (template == null)
            {
                LastError = "Template not found";
                return false;
            }

            if (template.IsSystemTemplate)
            {
                LastError = "Cannot modify system templates";
                return false;
            }

            await client
                .From<MeetingTemplateDetail>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, templateId.ToString())
                .Set(t => t.DefaultAgendaJson!, SerializeTemplateItems(items))
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log($"Updated template items: {templateId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error updating template items: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes a template (soft delete).
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var template = await GetTemplateAsync(templateId);
            if (template == null)
            {
                LastError = "Template not found";
                return false;
            }

            if (template.IsSystemTemplate)
            {
                LastError = "Cannot delete system templates";
                return false;
            }

            await client
                .From<MeetingTemplateDetail>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, templateId.ToString())
                .Set(t => t.IsDeleted, true)
                .Set(t => t.DeletedAt!, DateTime.UtcNow)
                .Update();

            Log($"Deleted template: {templateId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error deleting template: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Apply Template

    /// <summary>
    /// Applies a template to a meeting, creating agenda items.
    /// </summary>
    public async Task<bool> ApplyTemplateToMeetingAsync(Guid templateId, Guid meetingId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var template = await GetTemplateAsync(templateId);
            if (template == null)
            {
                LastError = "Template not found";
                return false;
            }

            var profile = AuthService.Instance.CurrentProfile;
            if (profile == null || !profile.OrganizationId.HasValue)
            {
                LastError = "No profile or organization";
                return false;
            }

            // Get existing agenda items to determine sort order
            var existingItems = await MeetingAgendaItemService.Instance.GetAgendaItemsForMeetingAsync(meetingId);
            int startOrder = existingItems.Count;

            // Create agenda items from template
            foreach (var templateItem in template.Items.OrderBy(i => i.SortOrder))
            {
                var agendaItem = new MeetingAgendaItem
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = profile.OrganizationId.Value,
                    MeetingId = meetingId,
                    AddedBy = profile.Id,
                    Title = templateItem.Title,
                    Description = templateItem.Description,
                    Status = "open",
                    SortOrder = startOrder++,
                    IsPrivate = false,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await client.From<MeetingAgendaItem>().Insert(agendaItem);
            }

            Log($"Applied template '{template.Name}' to meeting {meetingId} ({template.Items.Count} items)");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error applying template: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Default Templates

    /// <summary>
    /// Gets the default templates that should be available.
    /// </summary>
    public static List<DefaultTemplate> GetDefaultTemplates()
    {
        return new List<DefaultTemplate>
        {
            new DefaultTemplate
            {
                Name = "1:1 Check-in",
                Description = "Standard one-on-one meeting structure for regular check-ins with team members.",
                MeetingType = TemplateCategory.OneOnOne,
                Items = new List<DefaultTemplateItem>
                {
                    new("Personal Check-in", "How are you doing? Any wins or challenges to share?", false, 5),
                    new("Progress Updates", "Review current priorities and progress", false, 10),
                    new("Blockers & Support Needed", "What's getting in your way? How can I help?", false, 10),
                    new("Goals & Development", "Career growth, learning, skill development", true, 5),
                    new("Feedback Exchange", "Two-way feedback - what's working, what could improve", true, 5),
                    new("Action Items & Next Steps", "Capture decisions and follow-ups", false, 5)
                }
            },
            new DefaultTemplate
            {
                Name = "Team Standup",
                Description = "Quick team sync to align on priorities and blockers.",
                MeetingType = TemplateCategory.Team,
                Items = new List<DefaultTemplateItem>
                {
                    new("Yesterday's Accomplishments", "What did we complete?", false, 5),
                    new("Today's Priorities", "What are we focusing on?", false, 5),
                    new("Blockers & Dependencies", "What's blocking progress?", false, 5),
                    new("Announcements", "Team updates or FYIs", true, 3)
                }
            },
            new DefaultTemplate
            {
                Name = "Project Review",
                Description = "Periodic project status review and planning session.",
                MeetingType = TemplateCategory.Project,
                Items = new List<DefaultTemplateItem>
                {
                    new("Project Status Overview", "Current phase, timeline, key metrics", false, 10),
                    new("Milestone Progress", "Review completed and upcoming milestones", false, 10),
                    new("Risks & Issues", "Identify and discuss project risks", false, 10),
                    new("Resource & Budget Review", "Team capacity, spending, needs", true, 5),
                    new("Stakeholder Updates", "Communication needs, feedback received", true, 5),
                    new("Decisions Needed", "Items requiring decision or escalation", false, 10),
                    new("Next Steps & Action Items", "Capture assignments and deadlines", false, 5)
                }
            },
            new DefaultTemplate
            {
                Name = "Performance Review",
                Description = "Structured performance discussion template.",
                MeetingType = TemplateCategory.OneOnOne,
                Items = new List<DefaultTemplateItem>
                {
                    new("Review Period Highlights", "Key accomplishments and contributions", false, 15),
                    new("Goals Achievement", "Review progress on established goals", false, 10),
                    new("Strengths & Growth Areas", "What's working well, areas for development", false, 10),
                    new("Feedback Discussion", "360 feedback themes and observations", false, 10),
                    new("Next Period Goals", "Set objectives for the coming period", false, 10),
                    new("Development Plan", "Training, mentoring, career growth", false, 5)
                }
            },
            new DefaultTemplate
            {
                Name = "Sprint Retrospective",
                Description = "Agile retrospective for continuous improvement.",
                MeetingType = TemplateCategory.Team,
                Items = new List<DefaultTemplateItem>
                {
                    new("What Went Well", "Celebrate successes and things to keep doing", false, 10),
                    new("What Could Improve", "Identify areas for improvement", false, 10),
                    new("Action Items", "Concrete steps to improve next sprint", false, 10),
                    new("Team Health Check", "How is the team feeling?", true, 5)
                }
            }
        };
    }

    /// <summary>
    /// Ensures default templates exist for the organization.
    /// Called on first load or when templates are needed.
    /// </summary>
    public async Task EnsureDefaultTemplatesAsync()
    {
        var existing = await GetTemplatesAsync();
        if (existing.Any(t => t.IsSystemTemplate))
        {
            Log("System templates already exist");
            return;
        }

        var defaults = GetDefaultTemplates();
        foreach (var template in defaults)
        {
            var items = template.Items.Select(i => (i.Title, i.Description, i.IsOptional, i.DurationMinutes)).ToList();
            await CreateTemplateAsync(template.Name, template.Description, template.MeetingType, items, isSystemTemplate: true);
        }

        Log("Created default templates");
    }

    #endregion
}

/// <summary>
/// Default template definition for seeding.
/// </summary>
public class DefaultTemplate
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MeetingType { get; set; } = TemplateCategory.Custom;
    public List<DefaultTemplateItem> Items { get; set; } = new();
}

/// <summary>
/// Default template item definition.
/// </summary>
public class DefaultTemplateItem
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool IsOptional { get; set; }
    public int? DurationMinutes { get; set; }

    public DefaultTemplateItem(string title, string? description, bool isOptional, int? durationMinutes)
    {
        Title = title;
        Description = description;
        IsOptional = isOptional;
        DurationMinutes = durationMinutes;
    }
}
