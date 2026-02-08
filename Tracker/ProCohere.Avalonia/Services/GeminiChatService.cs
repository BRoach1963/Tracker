using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces;
using ProCohere.Avalonia.Services.AI;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Chat provider implementation for Google Gemini API.
/// Uses the REST API for the free tier (gemini-1.5-flash).
/// Includes comprehensive function calling capabilities.
/// </summary>
public class GeminiChatService : IChatProvider, IDisposable
{
    #region Constants

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const int TimeoutSeconds = 30;
    private const int MaxSystemContextLength = 4000;
    
    /// <summary>
    /// Ordered list of Gemini models to try. First available model wins.
    /// When Google deprecates a model, it falls back to the next one.
    /// </summary>
    private static readonly string[] FallbackModels = new[]
    {
        "gemini-2.5-flash",      // Current preferred (as of Feb 2026)
        "gemini-2.0-flash",      // Fallback
        "gemini-1.5-flash",      // Legacy fallback
        "gemini-1.5-pro",        // Pro tier fallback
        "gemini-pro"             // Oldest stable
    };

    #endregion

    #region Fields

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private string _model;
    private int _currentModelIndex;
    private bool _disposed;

    #endregion

    #region Constructor

    public GeminiChatService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
        
        // Load API key from environment or appsettings.json
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
                  AppSettingsService.Instance.GetGeminiApiKey() ??
                  string.Empty;
        
        // Load model from settings, or use first fallback model
        var aiSettings = AppSettingsService.Instance.GetAISettings();
        _model = !string.IsNullOrEmpty(aiSettings.GeminiModel) ? aiSettings.GeminiModel : FallbackModels[0];
        _currentModelIndex = Array.IndexOf(FallbackModels, _model);
        if (_currentModelIndex < 0) _currentModelIndex = 0;
        
        System.Diagnostics.Debug.WriteLine($"[GeminiChat] Initialized with model: {_model}, API key present: {!string.IsNullOrEmpty(_apiKey)}");
    }

    #endregion

    #region IChatProvider Implementation

    public string ProviderName => "Google Gemini";
    public bool RequiresInternet => true;
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public async Task<string> GetResponseAsync(string prompt, string? systemContext = null, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { ChatMessage.User(prompt) };
        return await GetResponseAsync(messages, systemContext, cancellationToken);
    }

    public async Task<string> GetResponseAsync(IEnumerable<ChatMessage> messages, string? systemContext = null, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Gemini API key is not configured. Please set your API key in Settings.");
        }

        // Check budget limits
        var (canProceed, budgetMessage) = AIUsageTracker.Instance.CheckCanMakeRequest();
        if (!canProceed)
        {
            return $"🚫 {budgetMessage}";
        }

        try
        {
            // Build request with function calling tools
            var request = BuildRequest(messages, systemContext);
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/{_model}:generateContent?key={_apiKey}";
            
            System.Diagnostics.Debug.WriteLine($"[GeminiChat] Sending request to model: {_model}");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"[GeminiChat] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                // Check if this is a model-related error (404 = model not found/deprecated)
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var fallbackResult = await TryFallbackModelAsync(messages, systemContext, cancellationToken);
                    if (fallbackResult != null)
                    {
                        return fallbackResult;
                    }
                }
                
                var errorMessage = HandleApiError(response.StatusCode, responseBody);
                return errorMessage;
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var firstCandidate = geminiResponse?.Candidates?.FirstOrDefault();
            var firstPart = firstCandidate?.Content?.Parts?.FirstOrDefault();

            if (firstPart == null)
            {
                return "I apologize, but I couldn't generate a response. Please try again.";
            }

            // Handle function calling
            if (firstPart.FunctionCall != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[GeminiChat] AI requested function call: {firstPart.FunctionCall.Name}");

                    // Execute the function directly with JsonElement args
                    var functionResult = await AIFunctionService.Instance.ExecuteFunctionAsync(
                        firstPart.FunctionCall.Name, 
                        firstPart.FunctionCall.Args);

                    System.Diagnostics.Debug.WriteLine($"[GeminiChat] Function result: {functionResult}");

                    // Create a follow-up request to get a natural language response
                    var messagesWithFunction = messages.ToList();
                    messagesWithFunction.Add(ChatMessage.Assistant("Calling function..."));
                    messagesWithFunction.Add(ChatMessage.User($"Function '{firstPart.FunctionCall.Name}' completed with result: {functionResult}. Please provide a natural language summary of what was accomplished."));

                    // Make recursive call for final response
                    return await GetResponseAsync(messagesWithFunction, systemContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeminiChat] Error executing function: {firstPart.FunctionCall.Name} - {ex.Message}");
                    return $"I attempted to {firstPart.FunctionCall.Name} but encountered an error: {ex.Message}";
                }
            }

            var text = firstPart.Text;
            if (string.IsNullOrEmpty(text))
            {
                return "I apologize, but I couldn't generate a response. Please try again.";
            }

            // Track usage for billing estimates
            AIUsageTracker.Instance.RecordRequest(jsonContent.Length / 4, text.Length / 4);

            return text;
        }
        catch (TaskCanceledException)
        {
            return "Request timed out. Please try again.";
        }
        catch (HttpRequestException)
        {
            return "I'm having trouble connecting to the internet. Please check your connection and try again.";
        }
        catch (Exception ex)
        {
            return $"An error occurred: {ex.Message}";
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Attempts to use fallback models when the current model returns 404.
    /// This handles Google deprecating models without breaking production.
    /// </summary>
    private async Task<string?> TryFallbackModelAsync(IEnumerable<ChatMessage> messages, string? systemContext, CancellationToken cancellationToken)
    {
        var originalModel = _model;
        
        // Try each fallback model starting from current position
        for (int i = _currentModelIndex + 1; i < FallbackModels.Length; i++)
        {
            var fallbackModel = FallbackModels[i];
            System.Diagnostics.Debug.WriteLine($"[GeminiChat] Model '{_model}' failed (404). Trying fallback: {fallbackModel}");
            
            try
            {
                // Update model for this attempt
                _model = fallbackModel;
                _currentModelIndex = i;
                
                var request = BuildRequest(messages, systemContext);
                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var url = $"{BaseUrl}/{_model}:generateContent?key={_apiKey}";
                
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeminiChat] Fallback to '{fallbackModel}' succeeded!");
                    
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Track usage
                        AIUsageTracker.Instance.RecordRequest(jsonContent.Length / 4, text.Length / 4);
                        return text;
                    }
                }
                else if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    // Different error, stop trying
                    break;
                }
                // 404 = model not found, continue to next fallback
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeminiChat] Fallback '{fallbackModel}' failed: {ex.Message}");
            }
        }
        
        // Restore original model if all fallbacks failed
        _model = originalModel;
        System.Diagnostics.Debug.WriteLine($"[GeminiChat] All fallback models exhausted. Restored to: {_model}");
        return null;
    }

    private GeminiRequest BuildRequest(IEnumerable<ChatMessage> messages, string? systemContext)
    {
        var request = new GeminiRequest
        {
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = 8192,
                Temperature = 0.7,
                TopP = 0.8,
                TopK = 40
            },
            SafetySettings = new List<GeminiSafetySetting>
            {
                new() { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new() { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new() { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new() { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" }
            }
        };

        // Add system context
        if (!string.IsNullOrEmpty(systemContext))
        {
            // Truncate system context if too long
            var truncatedContext = systemContext.Length > MaxSystemContextLength 
                ? systemContext.Substring(0, MaxSystemContextLength) + "..."
                : systemContext;
            
            request.SystemInstruction = new GeminiContent
            {
                Role = "system",
                Parts = new List<GeminiPart> { new() { Text = truncatedContext } }
            };
        }

        // Add conversation messages
        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                "user" => "user",
                "assistant" => "model", 
                "system" => "system",
                _ => "user"
            };

            request.Contents.Add(new GeminiContent
            {
                Role = role,
                Parts = new List<GeminiPart> { new() { Text = message.Content ?? "" } }
            });
        }

        request.Tools = BuildTools();

        return request;
    }

    private List<GeminiTool> BuildTools()
    {
        return new List<GeminiTool>
        {
            new GeminiTool
            {
                FunctionDeclarations = new List<GeminiFunctionDeclaration>
                {
                    // Task management functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_task",
                        Description = "Creates a new task with description, priority, and optional due date",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["description"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Clear description of what needs to be done"
                                },
                                ["priority"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Task priority: 'High', 'Medium', or 'Low'",
                                    Enum = new List<string> { "High", "Medium", "Low" }
                                },
                                ["due_date"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Optional due date in YYYY-MM-DD format"
                                },
                                ["assigned_to"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Optional person to assign the task to"
                                }
                            },
                            Required = new List<string> { "description" }
                        }
                    },

                    // Meeting management functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_meeting",
                        Description = "Schedules a new meeting with title, attendees, and agenda",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["title"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Meeting title or purpose"
                                },
                                ["attendees"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Comma-separated list of attendees"
                                },
                                ["date_time"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Meeting date and time"
                                },
                                ["agenda"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Meeting agenda or topics to discuss"
                                }
                            },
                            Required = new List<string> { "title" }
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "search_meetings",
                        Description = "Searches for meetings by attendee name, including past meetings. Use this to find when you last met with someone.",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["attendee_name"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Name of the attendee to search for (e.g., 'Janet', 'John Smith')"
                                },
                                ["upcoming_only"] = new GeminiProperty
                                {
                                    Type = "boolean",
                                    Description = "If true, only return upcoming meetings. If false (default), include past meetings."
                                },
                                ["limit"] = new GeminiProperty
                                {
                                    Type = "integer",
                                    Description = "Maximum number of meetings to return (default 10)"
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    // Goal management functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_goal",
                        Description = "Creates a new goal with title, description, and target date",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["title"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Goal title or objective"
                                },
                                ["description"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Detailed description of the goal"
                                },
                                ["target_date"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Target completion date"
                                },
                                ["category"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Goal category",
                                    Enum = new List<string> { "Personal", "Professional", "Team", "Learning", "Health" }
                                }
                            },
                            Required = new List<string> { "title" }
                        }
                    },

                    // Project management functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_project",
                        Description = "Creates a new project with name, description, and timeline",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["name"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Project name"
                                },
                                ["description"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Project description and objectives"
                                },
                                ["start_date"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Project start date"
                                },
                                ["end_date"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Project target end date"
                                }
                            },
                            Required = new List<string> { "name" }
                        }
                    },

                    // Note management functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_note",
                        Description = "Creates a new note or documentation entry",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["title"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Note title"
                                },
                                ["content"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Note content and details"
                                },
                                ["tags"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Comma-separated tags for organization"
                                }
                            },
                            Required = new List<string> { "title", "content" }
                        }
                    },

                    // Information retrieval functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "search_team_members",
                        Description = "Searches for team members by name or role",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["query"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Search term for name, role, or department"
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "get_upcoming_meetings",
                        Description = "Gets upcoming meetings for the specified number of days",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["days_ahead"] = new GeminiProperty
                                {
                                    Type = "integer",
                                    Description = "Number of days ahead to look (default: 7)"
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "get_projects",
                        Description = "Gets list of projects, optionally filtered by query",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["query"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Optional search query for project name"
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "get_notes",
                        Description = "Gets recent notes and documentation",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["limit"] = new GeminiProperty
                                {
                                    Type = "integer",
                                    Description = "Maximum number of notes to return (default: 10)"
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "get_tasks",
                        Description = "Gets task list with optional filters",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["priority"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Filter by priority: 'High', 'Medium', or 'Low'",
                                    Enum = new List<string> { "High", "Medium", "Low" }
                                },
                                ["status"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Filter by status: 'open', 'completed', 'all'",
                                    Enum = new List<string> { "open", "completed", "all" }
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    // Feedback function
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_feedback",
                        Description = "Creates feedback for a team member",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["team_member_name"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Name of the team member receiving feedback"
                                },
                                ["title"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Brief title/summary of the feedback"
                                },
                                ["content"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Detailed feedback content"
                                },
                                ["type"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Type of feedback: 'praise', 'constructive', 'coaching', 'recognition'",
                                    Enum = new List<string> { "praise", "constructive", "coaching", "recognition" }
                                }
                            },
                            Required = new List<string> { "team_member_name", "title", "content" }
                        }
                    },

                    // Metric function
                    new GeminiFunctionDeclaration
                    {
                        Name = "create_metric",
                        Description = "Creates a new metric/KPI to track",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["name"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Name of the metric"
                                },
                                ["target_value"] = new GeminiProperty
                                {
                                    Type = "number",
                                    Description = "Target value to achieve"
                                },
                                ["unit"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Unit of measurement (e.g., '%', 'hours', 'count')"
                                },
                                ["current_value"] = new GeminiProperty
                                {
                                    Type = "number",
                                    Description = "Current value (defaults to 0)"
                                },
                                ["description"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Optional description of what this metric measures"
                                }
                            },
                            Required = new List<string> { "name", "target_value" }
                        }
                    },

                    // Insights functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "get_insights",
                        Description = "Gets proactive AI insights - alerts about meeting gaps, goals at risk, metrics off target, and tasks that need attention",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["severity"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "Optional filter by severity: 'critical', 'high', 'medium', 'low', or 'all' (default)",
                                    Enum = new List<string> { "all", "critical", "high", "medium", "low" }
                                }
                            },
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "dismiss_insight",
                        Description = "Dismisses/acknowledges a specific insight so it no longer appears",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>
                            {
                                ["insight_id"] = new GeminiProperty
                                {
                                    Type = "string",
                                    Description = "The ID of the insight to dismiss"
                                }
                            },
                            Required = new List<string> { "insight_id" }
                        }
                    },

                    // Utility functions
                    new GeminiFunctionDeclaration
                    {
                        Name = "get_current_time",
                        Description = "Gets the current date and time",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>(),
                            Required = new List<string>()
                        }
                    },

                    new GeminiFunctionDeclaration
                    {
                        Name = "help",
                        Description = "Shows available AI functions and their usage",
                        Parameters = new GeminiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiProperty>(),
                            Required = new List<string>()
                        }
                    }
                }
            }
        };
    }

    private string HandleApiError(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        // Log the full error for debugging
        System.Diagnostics.Debug.WriteLine($"[GeminiChat] API Error: {statusCode}");
        System.Diagnostics.Debug.WriteLine($"[GeminiChat] Response body: {responseBody}");
        
        // Try to extract error message from response
        string? errorDetail = null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorObj))
            {
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    errorDetail = msgProp.GetString();
                }
            }
        }
        catch { /* Ignore JSON parsing errors */ }
        
        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid API key. Please check your Gemini API key in Settings.",
            System.Net.HttpStatusCode.Forbidden => $"Access denied. {errorDetail ?? "Check your API key permissions."}",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again in a few moments.",
            System.Net.HttpStatusCode.BadRequest => $"Invalid request: {errorDetail ?? "Please try rephrasing your message."}",
            System.Net.HttpStatusCode.NotFound => "API endpoint not found. The model may not be available.",
            System.Net.HttpStatusCode.ServiceUnavailable => "Gemini service is temporarily unavailable. Please try again later.",
            _ => $"API error ({(int)statusCode} {statusCode}): {errorDetail ?? "Please try again later."}"
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    #endregion

    #region Response Models

    private class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiContent? SystemInstruction { get; set; }
        public GeminiGenerationConfig? GenerationConfig { get; set; }
        public List<GeminiSafetySetting>? SafetySettings { get; set; }
        public List<GeminiTool>? Tools { get; set; }
    }

    private class GeminiContent
    {
        public string Role { get; set; } = "user";
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
        public GeminiFunctionCall? FunctionCall { get; set; }
    }

    private class GeminiFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public JsonElement Args { get; set; }
    }

    private class GeminiTool
    {
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    private class GeminiFunctionDeclaration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GeminiSchema? Parameters { get; set; }
    }

    private class GeminiSchema
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, GeminiProperty>? Properties { get; set; }
        public List<string>? Required { get; set; }
    }

    private class GeminiProperty
    {
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? Enum { get; set; }
    }

    private class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int TopK { get; set; }
    }

    private class GeminiSafetySetting
    {
        public string Category { get; set; } = string.Empty;
        public string Threshold { get; set; } = string.Empty;
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    #endregion
}