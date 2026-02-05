using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Repository for help content storage operations.
/// Handles file I/O and content persistence.
/// </summary>
public class HelpContentRepository : IHelpContentRepository
{
    private readonly string _helpDirectory;
    
    public HelpContentRepository()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _helpDirectory = Path.Combine(baseDirectory, "Help");
    }
    
    public async Task<IEnumerable<HelpTopic>> LoadTopicsAsync()
    {
        EnsureHelpDirectoryExists();
        
        var indexFile = Path.Combine(_helpDirectory, "index.json");
        if (!File.Exists(indexFile))
        {
            await CreateDefaultTopicIndexAsync();
        }
        
        var indexJson = await File.ReadAllTextAsync(indexFile);
        var topics = JsonSerializer.Deserialize<HelpTopic[]>(indexJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return topics ?? Enumerable.Empty<HelpTopic>();
    }
    
    public async Task<string> LoadTopicContentAsync(string filePath)
    {
        var fullPath = Path.Combine(_helpDirectory, filePath);
        if (!File.Exists(fullPath))
        {
            return string.Empty;
        }
        
        return await File.ReadAllTextAsync(fullPath);
    }
    
    public async Task SaveTopicIndexAsync(IEnumerable<HelpTopic> topics)
    {
        EnsureHelpDirectoryExists();
        
        var indexFile = Path.Combine(_helpDirectory, "index.json");
        var json = JsonSerializer.Serialize(topics, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await File.WriteAllTextAsync(indexFile, json);
    }
    
    public async Task CreateDefaultContentAsync()
    {
        EnsureHelpDirectoryExists();
        var topicsDir = Path.Combine(_helpDirectory, "topics");
        Directory.CreateDirectory(topicsDir);
        
        await CreateDefaultTopicFiles();
        await CreateDefaultTopicIndexAsync();
    }
    
    private void EnsureHelpDirectoryExists()
    {
        if (!Directory.Exists(_helpDirectory))
        {
            Directory.CreateDirectory(_helpDirectory);
        }
    }
    
    private async Task CreateDefaultTopicFiles()
    {
        var topicsDir = Path.Combine(_helpDirectory, "topics");
        
        var topicContents = new Dictionary<string, string>
        {
            ["overview.md"] = @"# ProCohere Help

Welcome to ProCohere! This help system provides guidance for using the application effectively.",

            ["briefing.md"] = @"# Briefing - Your Daily Dashboard

The Briefing shows what requires your attention right now.

## Manager Briefing
- Team member updates and attention needed
- Upcoming 1-on-1 meetings and agenda items  
- Team goals and metrics requiring review

## Individual Contributor Briefing
- Your tasks and deadlines for today/this week
- Goal progress and upcoming milestones
- Meeting preparations and action items",

            ["me-view.md"] = @"# Me - Your Personal Hub

The Me view is your personal workspace for managing tasks, goals, and progress.

## Features
- All your assigned and personal tasks
- Individual goals and objectives
- Meeting notes and action items
- Performance insights and feedback",

            ["pulse-view.md"] = @"# Pulse - Synthesis Hub

Pulse provides quick access to goals, metrics, and tasks with intelligent insights.

## Quick Access Tabs
- Goals overview and progress tracking
- Metrics and performance indicators  
- Task management and completion
- Trend analysis and recommendations",

            ["settings.md"] = @"# Settings - Customize Your Experience

Configure ProCohere to match your preferences.

## Account Settings
- Profile information and preferences
- Security and authentication
- Notification settings
- Theme and appearance options"
        };
        
        // Write all topic files
        foreach (var (fileName, content) in topicContents)
        {
            await File.WriteAllTextAsync(Path.Combine(topicsDir, fileName), content);
        }
    }
    
    private async Task CreateDefaultTopicIndexAsync()
    {
        var topics = new[]
        {
            new HelpTopic
            {
                Id = "overview",
                Title = "ProCohere Overview",
                Category = "Getting Started",
                Keywords = new List<string> { "overview", "introduction", "getting started", "help", "welcome", "navigation" },
                FilePath = "topics/overview.md",
                Priority = 100,
                IsContextSensitive = false,
                RelatedTopics = new List<string> { "briefing", "me-view", "pulse-view", "settings" }
            },
            new HelpTopic
            {
                Id = "briefing",
                Title = "Briefing - Your Daily Dashboard",
                Category = "Core Features",
                Keywords = new List<string> { "briefing", "dashboard", "today", "daily", "attention", "priorities", "manager", "ic" },
                FilePath = "topics/briefing.md",
                Priority = 95,
                IsContextSensitive = true,
                Contexts = new List<string> { "BriefingView", "ManagerBriefingContent", "ICBriefingContent", "MainWindowViewModel" },
                RelatedTopics = new List<string> { "me-view", "pulse-view", "circle-view" }
            },
            new HelpTopic
            {
                Id = "me-view",
                Title = "Me - Your Personal Hub",
                Category = "Core Features",
                Keywords = new List<string> { "me", "personal", "hub", "tasks", "goals", "meetings", "feedback", "individual" },
                FilePath = "topics/me-view.md",
                Priority = 90,
                IsContextSensitive = true,
                Contexts = new List<string> { "MeView", "MeViewModel" },
                RelatedTopics = new List<string> { "tasks", "goals", "briefing", "pulse-view" }
            },
            new HelpTopic
            {
                Id = "pulse-view",
                Title = "Pulse - Synthesis Hub",
                Category = "Core Features",
                Keywords = new List<string> { "pulse", "synthesis", "signals", "quick access", "overview", "trends", "analysis" },
                FilePath = "topics/pulse-view.md",
                Priority = 87,
                IsContextSensitive = true,
                Contexts = new List<string> { "PulseView", "PulseViewModel", "GoalsTabView", "MetricsTabView", "TasksTabView" },
                RelatedTopics = new List<string> { "goals", "metrics", "tasks", "briefing" }
            },
            new HelpTopic
            {
                Id = "settings",
                Title = "Settings - Customize Your Experience",
                Category = "Configuration",
                Keywords = new List<string> { "settings", "preferences", "configuration", "account", "profile", "notifications", "theme" },
                FilePath = "topics/settings.md",
                Priority = 70,
                IsContextSensitive = true,
                Contexts = new List<string> { "SettingsView", "SettingsViewModel", "ProfileDialog", "PreferencesDialog" },
                RelatedTopics = new List<string> { "overview", "briefing", "me-view" }
            }
        };
        
        await SaveTopicIndexAsync(topics);
    }
}