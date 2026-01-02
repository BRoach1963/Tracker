# Feature 06: Recognition & Kudos System
## Technical Specification

**Feature ID:** F-006  
**Priority:** P1  
**Estimated Effort:** 2-3 sprints  
**Status:** Planning

---

## Executive Summary

Enable managers to send recognition and kudos to team members with **external delivery** via Microsoft Teams, Slack, or Email. Since team members don't use Tracker directly, the kudos are composed in Tracker and delivered through the team member's preferred communication channel.

**Key Differentiator:** Managers get a history of recognition given, can track frequency, and receive AI prompts to recognize team members who haven't received kudos recently.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want to send kudos to a team member that gets delivered via Teams/Slack/Email | P0 |
| US-002 | As a manager, I want to see a history of kudos I've sent to each team member | P0 |
| US-003 | As a manager, I want AI to suggest team members who haven't been recognized recently | P1 |
| US-004 | As a manager, I want kudos templates for common achievements | P1 |
| US-005 | As a manager, I want to track recognition frequency across my team | P1 |
| US-006 | As a manager, I want to link kudos to specific accomplishments (tasks, OKRs) | P2 |
| US-007 | As a manager, I want kudos visible in meeting prep so I remember to mention them | P2 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       RECOGNITION & KUDOS SYSTEM                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    KudosService                                      │    │
│  │                                                                       │    │
│  │   ┌─────────────────┐    ┌─────────────────┐    ┌──────────────┐    │    │
│  │   │ KudosComposer   │───▶│ DeliveryManager │───▶│ DeliveryQueue│    │    │
│  │   │ (Create kudos)  │    │                 │    │              │    │    │
│  │   └─────────────────┘    └────────┬────────┘    └──────────────┘    │    │
│  │                                   │                                  │    │
│  │              ┌────────────────────┼────────────────────┐            │    │
│  │              │                    │                    │            │    │
│  │              ▼                    ▼                    ▼            │    │
│  │   ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐   │    │
│  │   │ TeamsDelivery    │ │ SlackDelivery    │ │ EmailDelivery    │   │    │
│  │   │ Provider         │ │ Provider         │ │ Provider         │   │    │
│  │   └──────────────────┘ └──────────────────┘ └──────────────────┘   │    │
│  │                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Recognition Analytics                             │    │
│  │                                                                       │    │
│  │   ┌─────────────────┐    ┌─────────────────┐    ┌──────────────┐    │    │
│  │   │ KudosHistoryView│    │ RecognitionGap  │    │ AI Prompts   │    │    │
│  │   │                 │    │ Analyzer        │    │              │    │    │
│  │   └─────────────────┘    └─────────────────┘    └──────────────┘    │    │
│  │                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Data Model                                        │    │
│  │                                                                       │    │
│  │   kudos table                                                        │    │
│  │   ┌─────────────────────────────────────────────────────────────┐   │    │
│  │   │ id │ team_member_id │ message │ category │ delivered │ ...  │   │    │
│  │   └─────────────────────────────────────────────────────────────┘   │    │
│  │                                                                       │    │
│  │   kudos_templates table                                              │    │
│  │   ┌─────────────────────────────────────────────────────────────┐   │    │
│  │   │ id │ name │ message_template │ category │ is_custom         │   │    │
│  │   └─────────────────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. Data Models

```csharp
public class Kudos
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public TeamMember TeamMember { get; set; }
    
    // Content
    public string Message { get; set; }
    public string? Title { get; set; }  // Optional headline
    public KudosCategory Category { get; set; }
    
    // Linked items (optional)
    public int? LinkedTaskId { get; set; }
    public int? LinkedOkrId { get; set; }
    public int? LinkedMeetingId { get; set; }
    
    // Delivery
    public DeliveryChannel DeliveryChannel { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryError { get; set; }
    
    // Visibility
    public bool IsPublic { get; set; }  // CC to team channel
    public bool MentionInMeetingPrep { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }  // Schedule for later
}

public enum KudosCategory
{
    TeamWork,
    Innovation,
    Leadership,
    CustomerFocus,
    GoingAboveBeyond,
    ProblemSolving,
    LearningGrowth,
    Reliability,
    Communication,
    Other
}

public enum DeliveryChannel
{
    MicrosoftTeams,
    Slack,
    Email,
    InternalOnly  // Just log in Tracker, no external delivery
}

public enum DeliveryStatus
{
    Draft,
    Scheduled,
    Sending,
    Delivered,
    Failed
}

public class KudosTemplate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string MessageTemplate { get; set; }  // Can include {Name}, {Achievement}
    public KudosCategory Category { get; set; }
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; }
}

public class KudosStats
{
    public int TeamMemberId { get; set; }
    public string TeamMemberName { get; set; }
    public int TotalKudosCount { get; set; }
    public DateTime? LastKudosDate { get; set; }
    public int DaysSinceLastKudos { get; set; }
    public Dictionary<KudosCategory, int> ByCategory { get; set; }
}
```

### 2. Kudos Service

```csharp
public class KudosService
{
    private readonly TrackerDbManager _db;
    private readonly IKudosDeliveryProvider _teamsProvider;
    private readonly IKudosDeliveryProvider _slackProvider;
    private readonly IKudosDeliveryProvider _emailProvider;
    
    /// <summary>
    /// Create and optionally deliver a kudos.
    /// </summary>
    public async Task<Kudos> SendKudosAsync(
        int teamMemberId, 
        string message, 
        KudosCategory category,
        DeliveryChannel channel,
        KudosOptions? options = null)
    {
        var teamMember = await _db.GetTeamMemberAsync(teamMemberId);
        
        var kudos = new Kudos
        {
            TeamMemberId = teamMemberId,
            Message = message,
            Title = options?.Title,
            Category = category,
            DeliveryChannel = channel,
            LinkedTaskId = options?.LinkedTaskId,
            LinkedOkrId = options?.LinkedOkrId,
            LinkedMeetingId = options?.LinkedMeetingId,
            IsPublic = options?.IsPublic ?? false,
            MentionInMeetingPrep = options?.MentionInMeetingPrep ?? true,
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = options?.ScheduleFor,
            DeliveryStatus = options?.ScheduleFor.HasValue 
                ? DeliveryStatus.Scheduled 
                : DeliveryStatus.Sending
        };
        
        await _db.SaveKudosAsync(kudos);
        
        // Deliver immediately if not scheduled
        if (!options?.ScheduleFor.HasValue && channel != DeliveryChannel.InternalOnly)
        {
            await DeliverKudosAsync(kudos, teamMember);
        }
        
        return kudos;
    }
    
    /// <summary>
    /// Actually deliver the kudos via the configured channel.
    /// </summary>
    private async Task DeliverKudosAsync(Kudos kudos, TeamMember teamMember)
    {
        var provider = kudos.DeliveryChannel switch
        {
            DeliveryChannel.MicrosoftTeams => _teamsProvider,
            DeliveryChannel.Slack => _slackProvider,
            DeliveryChannel.Email => _emailProvider,
            _ => null
        };
        
        if (provider == null)
        {
            kudos.DeliveryStatus = DeliveryStatus.Delivered;
            kudos.DeliveredAt = DateTime.UtcNow;
            await _db.UpdateKudosAsync(kudos);
            return;
        }
        
        try
        {
            var message = BuildDeliveryMessage(kudos, teamMember);
            await provider.SendAsync(teamMember, message, kudos.IsPublic);
            
            kudos.DeliveryStatus = DeliveryStatus.Delivered;
            kudos.DeliveredAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            kudos.DeliveryStatus = DeliveryStatus.Failed;
            kudos.DeliveryError = ex.Message;
        }
        
        await _db.UpdateKudosAsync(kudos);
    }
    
    /// <summary>
    /// Get recognition statistics for all team members.
    /// </summary>
    public async Task<List<KudosStats>> GetKudosStatsAsync()
    {
        var teamMembers = await _db.GetAllTeamMembersAsync();
        var allKudos = await _db.GetAllKudosAsync();
        
        return teamMembers.Select(tm => new KudosStats
        {
            TeamMemberId = tm.Id,
            TeamMemberName = tm.FullName,
            TotalKudosCount = allKudos.Count(k => k.TeamMemberId == tm.Id),
            LastKudosDate = allKudos
                .Where(k => k.TeamMemberId == tm.Id)
                .Max(k => (DateTime?)k.CreatedAt),
            DaysSinceLastKudos = CalculateDaysSinceLastKudos(tm.Id, allKudos),
            ByCategory = allKudos
                .Where(k => k.TeamMemberId == tm.Id)
                .GroupBy(k => k.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        }).ToList();
    }
    
    /// <summary>
    /// Get team members who haven't received recognition recently.
    /// </summary>
    public async Task<List<TeamMember>> GetUnderrecognizedTeamMembersAsync(int dayThreshold = 30)
    {
        var stats = await GetKudosStatsAsync();
        
        return stats
            .Where(s => s.DaysSinceLastKudos >= dayThreshold || s.TotalKudosCount == 0)
            .OrderByDescending(s => s.DaysSinceLastKudos)
            .Select(s => _db.GetTeamMemberAsync(s.TeamMemberId).Result)
            .Where(tm => tm != null)
            .ToList()!;
    }
    
    /// <summary>
    /// Get kudos history for a specific team member.
    /// </summary>
    public async Task<List<Kudos>> GetKudosHistoryAsync(int teamMemberId)
    {
        return await _db.GetKudosByTeamMemberAsync(teamMemberId);
    }
    
    /// <summary>
    /// Get recent kudos to show in meeting prep.
    /// </summary>
    public async Task<List<Kudos>> GetRecentKudosForMeetingPrepAsync(int teamMemberId, int days = 30)
    {
        var kudos = await _db.GetKudosByTeamMemberAsync(teamMemberId);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        
        return kudos
            .Where(k => k.MentionInMeetingPrep && k.CreatedAt >= cutoff)
            .OrderByDescending(k => k.CreatedAt)
            .Take(5)
            .ToList();
    }
    
    private KudosDeliveryMessage BuildDeliveryMessage(Kudos kudos, TeamMember teamMember)
    {
        var emoji = GetCategoryEmoji(kudos.Category);
        
        return new KudosDeliveryMessage
        {
            RecipientEmail = teamMember.Email,
            RecipientName = teamMember.FirstName,
            Subject = kudos.Title ?? $"{emoji} You've received a kudos!",
            Body = kudos.Message,
            Category = kudos.Category.ToString(),
            CategoryEmoji = emoji,
            SenderName = "Your Manager",  // Or get from settings
            IsPublic = kudos.IsPublic
        };
    }
    
    private string GetCategoryEmoji(KudosCategory category) => category switch
    {
        KudosCategory.TeamWork => "🤝",
        KudosCategory.Innovation => "💡",
        KudosCategory.Leadership => "⭐",
        KudosCategory.CustomerFocus => "❤️",
        KudosCategory.GoingAboveBeyond => "🚀",
        KudosCategory.ProblemSolving => "🧩",
        KudosCategory.LearningGrowth => "📈",
        KudosCategory.Reliability => "🎯",
        KudosCategory.Communication => "💬",
        _ => "🎉"
    };
}

public class KudosOptions
{
    public string? Title { get; set; }
    public int? LinkedTaskId { get; set; }
    public int? LinkedOkrId { get; set; }
    public int? LinkedMeetingId { get; set; }
    public bool IsPublic { get; set; }
    public bool MentionInMeetingPrep { get; set; } = true;
    public DateTime? ScheduleFor { get; set; }
}
```

### 3. Delivery Providers

#### IKudosDeliveryProvider Interface

```csharp
public interface IKudosDeliveryProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    
    Task<bool> TestConnectionAsync();
    Task SendAsync(TeamMember recipient, KudosDeliveryMessage message, bool isPublic);
}

public class KudosDeliveryMessage
{
    public string RecipientEmail { get; set; }
    public string RecipientName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Category { get; set; }
    public string CategoryEmoji { get; set; }
    public string SenderName { get; set; }
    public bool IsPublic { get; set; }
}
```

#### Microsoft Teams Provider

```csharp
public class TeamsKudosDeliveryProvider : IKudosDeliveryProvider
{
    private readonly string? _webhookUrl;
    private readonly string? _publicChannelWebhook;
    
    public string ProviderId => "teams";
    public string DisplayName => "Microsoft Teams";
    public bool IsConfigured => !string.IsNullOrEmpty(_webhookUrl);
    
    public async Task SendAsync(TeamMember recipient, KudosDeliveryMessage message, bool isPublic)
    {
        var card = BuildAdaptiveCard(message);
        
        // Send to individual via webhook or personal chat
        // Note: For personal messages, may need Graph API with proper permissions
        // Using incoming webhook to channel that user is in
        
        using var client = new HttpClient();
        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = card
                }
            }
        };
        
        var response = await client.PostAsJsonAsync(_webhookUrl, payload);
        response.EnsureSuccessStatusCode();
        
        // Also post to public channel if requested
        if (isPublic && !string.IsNullOrEmpty(_publicChannelWebhook))
        {
            var publicCard = BuildPublicAdaptiveCard(message);
            var publicPayload = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = publicCard
                    }
                }
            };
            
            await client.PostAsJsonAsync(_publicChannelWebhook, publicPayload);
        }
    }
    
    private object BuildAdaptiveCard(KudosDeliveryMessage message)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = $"{message.CategoryEmoji} {message.Subject}",
                    weight = "bolder",
                    size = "medium"
                },
                new
                {
                    type = "TextBlock",
                    text = $"Hi {message.RecipientName}!",
                    wrap = true
                },
                new
                {
                    type = "TextBlock",
                    text = message.Body,
                    wrap = true
                },
                new
                {
                    type = "FactSet",
                    facts = new[]
                    {
                        new { title = "Category", value = $"{message.CategoryEmoji} {message.Category}" },
                        new { title = "From", value = message.SenderName }
                    }
                }
            }
        };
    }
    
    private object BuildPublicAdaptiveCard(KudosDeliveryMessage message)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = $"{message.CategoryEmoji} Kudos Alert!",
                    weight = "bolder",
                    size = "large"
                },
                new
                {
                    type = "TextBlock",
                    text = $"**{message.RecipientName}** just received recognition for **{message.Category}**!",
                    wrap = true
                },
                new
                {
                    type = "TextBlock",
                    text = $"_{message.Body}_",
                    wrap = true,
                    isSubtle = true
                }
            }
        };
    }
}
```

#### Slack Provider

```csharp
public class SlackKudosDeliveryProvider : IKudosDeliveryProvider
{
    private readonly string? _botToken;
    private readonly string? _publicChannelId;
    
    public string ProviderId => "slack";
    public string DisplayName => "Slack";
    public bool IsConfigured => !string.IsNullOrEmpty(_botToken);
    
    public async Task SendAsync(TeamMember recipient, KudosDeliveryMessage message, bool isPublic)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _botToken);
        
        // Find user by email
        var userId = await FindUserByEmailAsync(client, recipient.Email);
        if (userId == null)
            throw new Exception($"Slack user not found for {recipient.Email}");
        
        // Send DM
        var dmPayload = new
        {
            channel = userId,
            text = $"{message.CategoryEmoji} {message.Subject}",
            blocks = BuildBlocks(message)
        };
        
        var response = await client.PostAsJsonAsync(
            "https://slack.com/api/chat.postMessage", 
            dmPayload
        );
        var result = await response.Content.ReadFromJsonAsync<SlackResponse>();
        
        if (!result.Ok)
            throw new Exception($"Slack send failed: {result.Error}");
        
        // Post to public channel if requested
        if (isPublic && !string.IsNullOrEmpty(_publicChannelId))
        {
            var publicPayload = new
            {
                channel = _publicChannelId,
                text = $"{message.CategoryEmoji} {message.RecipientName} received kudos!",
                blocks = BuildPublicBlocks(message)
            };
            
            await client.PostAsJsonAsync(
                "https://slack.com/api/chat.postMessage", 
                publicPayload
            );
        }
    }
    
    private async Task<string?> FindUserByEmailAsync(HttpClient client, string email)
    {
        var response = await client.GetAsync(
            $"https://slack.com/api/users.lookupByEmail?email={Uri.EscapeDataString(email)}"
        );
        var result = await response.Content.ReadFromJsonAsync<SlackUserResponse>();
        return result?.Ok == true ? result.User?.Id : null;
    }
    
    private object[] BuildBlocks(KudosDeliveryMessage message)
    {
        return new object[]
        {
            new
            {
                type = "header",
                text = new { type = "plain_text", text = $"{message.CategoryEmoji} {message.Subject}" }
            },
            new
            {
                type = "section",
                text = new { type = "mrkdwn", text = $"Hi {message.RecipientName}! :wave:" }
            },
            new
            {
                type = "section",
                text = new { type = "mrkdwn", text = message.Body }
            },
            new
            {
                type = "context",
                elements = new[]
                {
                    new { type = "mrkdwn", text = $"*Category:* {message.Category}" },
                    new { type = "mrkdwn", text = $"*From:* {message.SenderName}" }
                }
            }
        };
    }
}
```

#### Email Provider

```csharp
public class EmailKudosDeliveryProvider : IKudosDeliveryProvider
{
    private readonly SmtpSettings? _smtpSettings;
    
    public string ProviderId => "email";
    public string DisplayName => "Email";
    public bool IsConfigured => _smtpSettings?.IsConfigured == true;
    
    public async Task SendAsync(TeamMember recipient, KudosDeliveryMessage message, bool isPublic)
    {
        using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        smtpClient.Credentials = new NetworkCredential(
            _smtpSettings.Username, 
            _smtpSettings.Password
        );
        smtpClient.EnableSsl = _smtpSettings.UseSsl;
        
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
            Subject = $"{message.CategoryEmoji} {message.Subject}",
            Body = BuildHtmlBody(message),
            IsBodyHtml = true
        };
        
        mailMessage.To.Add(new MailAddress(recipient.Email, recipient.FullName));
        
        // CC to public distribution list if public
        if (isPublic && !string.IsNullOrEmpty(_smtpSettings.PublicDistributionList))
        {
            mailMessage.CC.Add(_smtpSettings.PublicDistributionList);
        }
        
        await smtpClient.SendMailAsync(mailMessage);
    }
    
    private string BuildHtmlBody(KudosDeliveryMessage message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6366f1, #8b5cf6); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; border-top: none; }}
        .category {{ display: inline-block; background: #e0e7ff; color: #3730a3; padding: 5px 15px; border-radius: 20px; font-size: 14px; margin-top: 15px; }}
        .footer {{ text-align: center; color: #64748b; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div style='font-size: 48px;'>{message.CategoryEmoji}</div>
            <h1 style='margin: 10px 0 0 0;'>You've Received Kudos!</h1>
        </div>
        <div class='content'>
            <p>Hi {message.RecipientName},</p>
            <p style='font-size: 18px;'>{message.Body}</p>
            <div class='category'>{message.CategoryEmoji} {message.Category}</div>
            <p style='margin-top: 30px;'>— {message.SenderName}</p>
        </div>
        <div class='footer'>
            <p>This recognition was sent via Tracker</p>
        </div>
    </div>
</body>
</html>";
    }
}
```

### 4. Database Schema

```sql
CREATE TABLE kudos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    team_member_id INTEGER NOT NULL,
    message TEXT NOT NULL,
    title TEXT,
    category TEXT NOT NULL,
    delivery_channel TEXT NOT NULL,
    delivery_status TEXT NOT NULL DEFAULT 'Draft',
    delivered_at TEXT,
    delivery_error TEXT,
    linked_task_id INTEGER,
    linked_okr_id INTEGER,
    linked_meeting_id INTEGER,
    is_public INTEGER NOT NULL DEFAULT 0,
    mention_in_meeting_prep INTEGER NOT NULL DEFAULT 1,
    scheduled_for TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (team_member_id) REFERENCES TeamMembers(Id),
    FOREIGN KEY (linked_task_id) REFERENCES Tasks(Id),
    FOREIGN KEY (linked_okr_id) REFERENCES Objectives(Id),
    FOREIGN KEY (linked_meeting_id) REFERENCES OneOnOnes(Id)
);

CREATE INDEX idx_kudos_team_member ON kudos(team_member_id);
CREATE INDEX idx_kudos_created ON kudos(created_at);
CREATE INDEX idx_kudos_delivery ON kudos(delivery_status);

CREATE TABLE kudos_templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    message_template TEXT NOT NULL,
    category TEXT NOT NULL,
    is_built_in INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Built-in templates
INSERT INTO kudos_templates (name, message_template, category, is_built_in) VALUES
('Great Teamwork', 'Thank you for your excellent collaboration on {Achievement}. Your teamwork made a real difference!', 'TeamWork', 1),
('Innovative Solution', 'I really appreciate your creative approach to solving {Achievement}. Your innovation is inspiring!', 'Innovation', 1),
('Going Above & Beyond', 'Thank you for going the extra mile on {Achievement}. Your dedication doesn''t go unnoticed!', 'GoingAboveBeyond', 1),
('Great Communication', 'Your clear and proactive communication on {Achievement} has been invaluable. Keep it up!', 'Communication', 1),
('Problem Solved', 'Great job tackling {Achievement}! Your problem-solving skills really shone through.', 'ProblemSolving', 1),
('Reliable Delivery', 'Thank you for consistently delivering quality work on {Achievement}. I can always count on you!', 'Reliability', 1);
```

### 5. UI Components

#### SendKudosDialog

```xaml
<Window Title="Send Kudos 🎉" Width="500" Height="600">
    <Grid>
        <!-- Team Member Selection -->
        <ComboBox x:Name="TeamMemberCombo" 
                  ItemsSource="{Binding TeamMembers}"
                  DisplayMemberPath="FullName"
                  SelectedItem="{Binding SelectedTeamMember}"/>
        
        <!-- Category Selection -->
        <ItemsControl ItemsSource="{Binding Categories}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <RadioButton Content="{Binding Display}" 
                                 GroupName="Category"
                                 IsChecked="{Binding IsSelected}"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <!-- Template Quick Pick -->
        <ComboBox ItemsSource="{Binding Templates}"
                  DisplayMemberPath="Name"
                  SelectionChanged="ApplyTemplate"/>
        
        <!-- Message -->
        <TextBox Text="{Binding Message}" 
                 AcceptsReturn="True"
                 Height="150"
                 PlaceholderText="Write your kudos message..."/>
        
        <!-- Link to Achievement -->
        <Expander Header="Link to Achievement (optional)">
            <StackPanel>
                <ComboBox ItemsSource="{Binding RecentTasks}" Header="Task"/>
                <ComboBox ItemsSource="{Binding RecentOkrs}" Header="OKR"/>
            </StackPanel>
        </Expander>
        
        <!-- Delivery Options -->
        <GroupBox Header="Delivery">
            <StackPanel>
                <ComboBox ItemsSource="{Binding DeliveryChannels}"
                          SelectedItem="{Binding SelectedChannel}"/>
                <CheckBox Content="Also post to team channel"
                          IsChecked="{Binding IsPublic}"
                          IsEnabled="{Binding CanBePublic}"/>
                <CheckBox Content="Include in next meeting prep"
                          IsChecked="{Binding MentionInMeetingPrep}"/>
            </StackPanel>
        </GroupBox>
        
        <!-- Actions -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Schedule for Later" Command="{Binding ScheduleCommand}"/>
            <Button Content="Send Now 🚀" Command="{Binding SendCommand}" IsDefault="True"/>
        </StackPanel>
    </Grid>
</Window>
```

#### KudosHistoryPanel

```xaml
<UserControl>
    <Grid>
        <!-- Summary Stats -->
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding TotalKudosSent}"/>
            <TextBlock Text="{Binding LastKudosDate, StringFormat='Last: {0:MMM d}'}"/>
        </StackPanel>
        
        <!-- History List -->
        <ListView ItemsSource="{Binding KudosHistory}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <Grid>
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="{Binding CategoryEmoji}" FontSize="20"/>
                            <StackPanel>
                                <TextBlock Text="{Binding TeamMember.FullName}" FontWeight="Bold"/>
                                <TextBlock Text="{Binding Message}" TextTrimming="CharacterEllipsis"/>
                            </StackPanel>
                        </StackPanel>
                        <StackPanel HorizontalAlignment="Right">
                            <TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:MMM d}'}"/>
                            <TextBlock Text="{Binding DeliveryStatus}"/>
                        </StackPanel>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</UserControl>
```

#### RecognitionGapAlert (for Daily Briefing)

```xaml
<DataTemplate x:Key="RecognitionGapInsight">
    <Border Background="#FEF3C7" CornerRadius="8" Padding="12">
        <Grid>
            <StackPanel>
                <TextBlock Text="⚠️ Recognition Gap" FontWeight="Bold"/>
                <TextBlock Text="{Binding Message}" TextWrapping="Wrap"/>
                <ItemsControl ItemsSource="{Binding TeamMembers}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Ellipse Width="24" Height="24">
                                    <Ellipse.Fill>
                                        <ImageBrush ImageSource="{Binding AvatarUrl}"/>
                                    </Ellipse.Fill>
                                </Ellipse>
                                <TextBlock Text="{Binding FullName}"/>
                                <TextBlock Text="{Binding DaysSinceKudos, StringFormat='{} ({0} days)'}"/>
                            </StackPanel>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
            <Button Content="Send Kudos" Command="{Binding SendKudosCommand}" 
                    HorizontalAlignment="Right"/>
        </Grid>
    </Border>
</DataTemplate>
```

### 6. AI Integration for Recognition Prompts

```csharp
public class RecognitionGapAnalyzer : IInsightAnalyzer
{
    public string AnalyzerId => "recognition_gap";
    
    public async Task<List<Insight>> AnalyzeAsync()
    {
        var insights = new List<Insight>();
        var stats = await _kudosService.GetKudosStatsAsync();
        
        // Find team members not recognized in 30+ days
        var underrecognized = stats
            .Where(s => s.DaysSinceLastKudos >= 30 || s.TotalKudosCount == 0)
            .OrderByDescending(s => s.DaysSinceLastKudos)
            .ToList();
        
        if (underrecognized.Any())
        {
            var names = string.Join(", ", underrecognized.Take(3).Select(s => s.TeamMemberName));
            var message = underrecognized.Count == 1
                ? $"{underrecognized[0].TeamMemberName} hasn't received recognition in {underrecognized[0].DaysSinceLastKudos} days."
                : $"{underrecognized.Count} team members haven't been recognized recently: {names}";
            
            insights.Add(new Insight
            {
                Type = InsightType.RecognitionGap,
                Priority = underrecognized.Max(s => s.DaysSinceLastKudos) > 60 
                    ? InsightPriority.High 
                    : InsightPriority.Medium,
                Title = "Recognition Gap Detected",
                Message = message,
                ActionLabel = "Send Kudos",
                ActionData = JsonSerializer.Serialize(underrecognized.Select(s => s.TeamMemberId)),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        
        return insights;
    }
}

// Add to meeting prep
public class FeedbackDataGatherer : IMeetingPrepGatherer
{
    public async Task<MeetingPrepSection> GatherAsync(TeamMember teamMember)
    {
        var recentKudos = await _kudosService.GetRecentKudosForMeetingPrepAsync(teamMember.Id);
        
        var items = recentKudos.Select(k => new MeetingPrepItem
        {
            Category = "Recognition Given",
            Content = $"{k.CategoryEmoji} {k.Category}: {k.Message.Truncate(100)}",
            Date = k.CreatedAt,
            Priority = MeetingPrepPriority.Low  // Informational
        }).ToList();
        
        return new MeetingPrepSection
        {
            Title = "Recent Recognition",
            Items = items,
            EmptyMessage = $"No recent kudos for {teamMember.FirstName}"
        };
    }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create Kudos data model | 2h | None |
| Create kudos database table | 1h | None |
| Create KudosService | 4h | Models, Database |
| Create IKudosDeliveryProvider interface | 1h | None |
| Create EmailKudosDeliveryProvider | 4h | Interface |
| Create built-in templates | 2h | Database |

### Phase 2: External Integrations (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create TeamsKudosDeliveryProvider | 6h | Interface |
| Create SlackKudosDeliveryProvider | 6h | Interface |
| Create delivery settings page | 4h | Providers |
| Test all delivery channels | 4h | All providers |

### Phase 3: UI & Analytics (Sprint 3)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create SendKudosDialog | 6h | KudosService |
| Create KudosHistoryPanel | 4h | KudosService |
| Add kudos button to team member detail | 2h | Dialog |
| Create RecognitionGapAnalyzer | 3h | KudosService |
| Integrate with Meeting Prep | 3h | KudosService |
| Add to Daily Briefing insights | 2h | Analyzer |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Teams webhook limitations | High | Use Graph API for DMs if needed |
| Slack rate limits | Medium | Implement retry with backoff |
| Email deliverability | Medium | Use proper SPF/DKIM, professional ESP |
| Finding user in Teams/Slack | High | Require email match, manual mapping option |

### Privacy & HR Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Public kudos inappropriate | Medium | Default to private, opt-in for public |
| Message content review | Low | Manager composes, they own content |
| Recognition tracking concerns | Medium | Focus on manager-side analytics only |

### Adoption Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Manager doesn't use it | High | AI prompts, easy quick-send |
| Team member ignores | Low | Can't control, but delivery confirmed |
| Over-recognition feels fake | Medium | Quality templates, AI spacing suggestions |

---

## Configuration

### Settings

```json
{
    "Recognition": {
        "DefaultDeliveryChannel": "teams",
        "DefaultIsPublic": false,
        "IncludeInMeetingPrep": true,
        "RecognitionGapDays": 30,
        "RecognitionGapAlertEnabled": true
    },
    "TeamsDelivery": {
        "WebhookUrl": "https://...",
        "PublicChannelWebhook": "https://...",
        "IsEnabled": true
    },
    "SlackDelivery": {
        "BotToken": "xoxb-...",
        "PublicChannelId": "C...",
        "IsEnabled": false
    },
    "EmailDelivery": {
        "SmtpHost": "smtp.example.com",
        "SmtpPort": 587,
        "UseSsl": true,
        "FromEmail": "recognition@company.com",
        "FromName": "Company Recognition",
        "IsEnabled": true
    }
}
```

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Kudos sent per manager/month | 4+ | Count kudos |
| Delivery success rate | >95% | Track delivery status |
| Recognition gap closure | 50% reduction | Track underrecognized count |
| Manager feature adoption | >70% | Track usage |

---

## Future Enhancements

1. **Peer Recognition** - If team members gain app access, enable peer kudos
2. **Recognition Badges** - Virtual badges for milestones
3. **Recognition Wall** - Public display (if org wants)
4. **Manager Recognition** - Remind to recognize upward too
5. **Anniversary Kudos** - Auto-prompt for work anniversaries
6. **AI Message Enhancement** - Suggest more impactful wording

---

**Document End**
