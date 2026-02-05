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
    private const string DefaultModel = "gemini-1.5-flash";
    private const int TimeoutSeconds = 30;
    private const int MaxSystemContextLength = 4000;

    #endregion

    #region Fields

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
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
            var url = $"{BaseUrl}/{DefaultModel}:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
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

    private string? LoadApiKeyFromSettings()
    {
        // TODO: Load from ProCohere settings system
        // For now, return null - will be implemented with settings integration
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

        // TODO: Add function calling tools in Phase 2
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
        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid API key. Please check your Gemini API key in Settings.",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again in a few moments.",
            System.Net.HttpStatusCode.BadRequest => "Invalid request. Please try rephrasing your message.",
            _ => $"API error ({statusCode}). Please try again later."
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