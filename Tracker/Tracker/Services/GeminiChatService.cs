using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.AI;

namespace Tracker.Services
{
    /// <summary>
    /// Chat provider implementation for Google Gemini API.
    /// Uses the REST API for the free tier (gemini-1.5-flash).
    /// </summary>
    public class GeminiChatService : IChatProvider, IDisposable
    {
        #region Constants

        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
        private const string DefaultModel = "gemini-2.5-pro";
        private const int DefaultMaxTokens = 1024;
        private const int TimeoutSeconds = 30;

        #endregion

        #region Fields

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private string _apiKey;
        private string _model;
        private int _maxTokens;
        private bool _disposed;

        #endregion

        #region Constructor

        public GeminiChatService()
        {
            _logger = LoggingManager.GetComponentLogger("GeminiChat");
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };

            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = UserSettingsManager.Instance.Settings.AI;
            _apiKey = settings.GeminiApiKey;
            _model = string.IsNullOrEmpty(settings.GeminiModel) ? DefaultModel : settings.GeminiModel;
            _maxTokens = settings.MaxResponseTokens > 0 ? settings.MaxResponseTokens : DefaultMaxTokens;
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
                // Log what we're sending BEFORE building request
                var totalMessageChars = messages.Sum(m => m.Content?.Length ?? 0);
                var systemContextChars = systemContext?.Length ?? 0;
                _logger.Info("PRE-REQUEST: Messages={0} chars, SystemContext={1} chars", totalMessageChars, systemContextChars);

                // HARD LIMIT: Truncate system context if too large
                if (systemContextChars > 4000)
                {
                    _logger.Warn("System context too large ({0}), truncating to 4000", systemContextChars);
                    systemContext = systemContext?.Substring(0, 4000);
                }

                var request = BuildRequest(messages, systemContext);
                var url = $"{BaseUrl}/{_model}:generateContent?key={_apiKey}";

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                _logger.Info("REQUEST SIZE: {0} chars total", jsonContent.Length);

                // Check if request is too large
                if (jsonContent.Length > 30000)
                {
                    _logger.Error("Request still too large ({0} chars) after limits!", jsonContent.Length);
                    return "I'm sorry, but the request is too large. Try asking a simpler question.";
                }

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.Debug("Gemini response status: {0}, body length: {1}", response.StatusCode, responseBody.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Gemini API error: {0} - {1}", response.StatusCode, responseBody);
                    
                    // Log more details for debugging
                    if (responseBody.Contains("INVALID_ARGUMENT"))
                    {
                        _logger.Error("Invalid argument - likely content too long. Request was {0} chars", jsonContent.Length);
                    }
                    
                    return HandleApiError(response.StatusCode, responseBody);
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var firstPart = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault();

                // Check if the AI wants to call a function
                if (firstPart?.FunctionCall != null)
                {
                    _logger.Info("AI requested function call: {0}", firstPart.FunctionCall.Name);
                    
                    // Execute the function
                    var functionResult = await AIFunctionService.Instance.ExecuteFunctionAsync(
                        firstPart.FunctionCall.Name, 
                        firstPart.FunctionCall.Args);

                    _logger.Info("Function result: {0}", functionResult);

                    // Send the function result back to the AI for a natural language response
                    var messagesWithFunction = messages.ToList();
                    messagesWithFunction.Add(new ChatMessage { Role = "model", Content = "" }); // AI's function call
                    messagesWithFunction.Add(new ChatMessage { Role = "function", Content = functionResult }); // Function result

                    // Make a second call to get the final response
                    return await GetResponseAsync(messagesWithFunction, systemContext, cancellationToken);
                }

                var text = firstPart?.Text;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.Warn("Gemini returned empty response");
                    return "I apologize, but I couldn't generate a response. Please try again.";
                }

                // Track usage for billing estimates
                AIUsageTracker.Instance.RecordRequest(jsonContent.Length, text.Length);

                _logger.Debug("Gemini response received: {0} chars", text.Length);
                return text;
            }
            catch (TaskCanceledException)
            {
                _logger.Warn("Gemini request was cancelled");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.Exception(ex, "Network error communicating with Gemini");
                return "I'm having trouble connecting to the internet. Please check your connection and try again.";
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error calling Gemini API");
                return $"An error occurred: {ex.Message}";
            }
        }

        #endregion

        #region Private Methods

        private List<GeminiTool> BuildTools()
        {
            return new List<GeminiTool>
            {
                new GeminiTool
                {
                    FunctionDeclarations = new List<GeminiFunctionDeclaration>
                    {
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_meeting",
                            Description = "Creates a new 1:1 meeting with a team member. Only requires name and date - no phone or other contact info needed.",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["team_member_name"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Name of the team member (first name or full name)"
                                    },
                                    ["date"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Date and time of the meeting in any standard format like '2025-12-24 2:00 PM' or '12/24/2025 14:00'. Parse relative dates yourself: 'next Tuesday' → calculate actual date. Today is " + DateTime.Now.ToString("yyyy-MM-dd (dddd)") + "."
                                    },
                                    ["notes"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional notes or agenda for the meeting"
                                    }
                                },
                                Required = new List<string> { "team_member_name", "date" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_task",
                            Description = "Creates a new task",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["description"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Description of the task"
                                    },
                                    ["owner_name"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional name of the team member who owns this task"
                                    },
                                    ["due_date"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional due date for the task"
                                    }
                                },
                                Required = new List<string> { "description" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_kpi",
                            Description = "Creates a new Key Performance Indicator (KPI)",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["name"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Name of the KPI"
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
                                    }
                                },
                                Required = new List<string> { "name", "target_value" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_okr",
                            Description = "Creates a new Objective and Key Results (OKR)",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["title"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Title/objective of the OKR"
                                    },
                                    ["description"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional detailed description"
                                    }
                                },
                                Required = new List<string> { "title" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "search_team_members",
                            Description = "Searches for team members by name, job title, or email",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["query"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Search query (optional, returns all if empty)"
                                    }
                                },
                                Required = new List<string>()
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "get_upcoming_meetings",
                            Description = "Gets scheduled upcoming 1:1 meetings",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["days_ahead"] = new GeminiProperty
                                    {
                                        Type = "integer",
                                        Description = "Number of days ahead to look (defaults to 7)"
                                    }
                                },
                                Required = new List<string>()
                            }
                        },
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
                                        Description = "Type of feedback: 'Positive', 'Constructive', 'Recognition', or 'Development'",
                                        Enum = new List<string> { "Positive", "Constructive", "Recognition", "Development" }
                                    }
                                },
                                Required = new List<string> { "team_member_name", "title", "content" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_project",
                            Description = "Creates a new project",
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
                                        Description = "Project description"
                                    },
                                    ["start_date"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional start date"
                                    },
                                    ["end_date"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional target end date"
                                    }
                                },
                                Required = new List<string> { "name" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_goal",
                            Description = "Creates an individual development goal for a team member",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["team_member_name"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Name of the team member"
                                    },
                                    ["title"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Goal title"
                                    },
                                    ["description"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Detailed description of the goal"
                                    },
                                    ["target_date"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional target completion date"
                                    },
                                    ["category"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Goal category: 'SkillDevelopment', 'Certification', 'CareerProgression', 'Leadership', or 'Personal'",
                                        Enum = new List<string> { "SkillDevelopment", "Certification", "CareerProgression", "Leadership", "Personal" }
                                    }
                                },
                                Required = new List<string> { "team_member_name", "title" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "create_note",
                            Description = "Creates a quick note or journal entry",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["content"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Note content"
                                    },
                                    ["title"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Optional note title"
                                    },
                                    ["category"] = new GeminiProperty
                                    {
                                        Type = "string",
                                        Description = "Category: 'General', 'Meeting', 'Idea', 'Todo', or 'Reminder'",
                                        Enum = new List<string> { "General", "Meeting", "Idea", "Todo", "Reminder" }
                                    }
                                },
                                Required = new List<string> { "content" }
                            }
                        },
                        new GeminiFunctionDeclaration
                        {
                            Name = "get_projects",
                            Description = "Gets all projects or searches by name",
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
                            Description = "Gets recent notes/journal entries",
                            Parameters = new GeminiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, GeminiProperty>
                                {
                                    ["limit"] = new GeminiProperty
                                    {
                                        Type = "integer",
                                        Description = "Maximum number of notes to return (defaults to 10)"
                                    }
                                },
                                Required = new List<string>()
                            }
                        }
                    }
                }
            };
        }

        private GeminiRequest BuildRequest(IEnumerable<ChatMessage> messages, string? systemContext)
        {
            var contents = new List<GeminiContent>();

            // Add conversation messages ONLY - system context goes in systemInstruction
            foreach (var message in messages)
            {
                contents.Add(new GeminiContent
                {
                    Role = message.Role == "assistant" ? "model" : "user",
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = message.Content }
                    }
                });
            }

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = _maxTokens,
                    Temperature = 0.7,
                    TopP = 0.95,
                    TopK = 40
                },
                SafetySettings = new List<GeminiSafetySetting>
                {
                    new GeminiSafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new GeminiSafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new GeminiSafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new GeminiSafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" }
                },
                Tools = BuildTools() // Add function calling tools
            };

            // Add system instruction separately (Gemini handles this more efficiently than stuffing in contents)
            if (!string.IsNullOrEmpty(systemContext))
            {
                request.SystemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = systemContext }
                    }
                };
            }

            return request;
        }

        private string HandleApiError(System.Net.HttpStatusCode statusCode, string responseBody)
        {
            // Try to extract error message from response
            var errorDetail = "";
            if (responseBody.Contains("\"message\""))
            {
                try
                {
                    var startIdx = responseBody.IndexOf("\"message\"") + 11;
                    var endIdx = responseBody.IndexOf("\"", startIdx);
                    if (endIdx > startIdx)
                    {
                        errorDetail = responseBody.Substring(startIdx, endIdx - startIdx);
                    }
                }
                catch { }
            }

            // Check for API key issues first (this is the most common problem)
            if (responseBody.Contains("API_KEY_INVALID") || responseBody.Contains("API key not valid"))
            {
                return "❌ Invalid API key. Please get a new key from Google AI Studio and update it in Settings → AI.";
            }

            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => 
                    "❌ Invalid API key. Please check your Gemini API key in Settings → AI.",
                System.Net.HttpStatusCode.TooManyRequests => 
                    "⏳ Rate limit exceeded. Please wait a moment and try again.",
                System.Net.HttpStatusCode.BadRequest when responseBody.Contains("exceeds the limit") => 
                    "📏 The request was too large. Try asking a shorter question.",
                System.Net.HttpStatusCode.BadRequest => 
                    $"Request error: {(string.IsNullOrEmpty(errorDetail) ? "Invalid format" : errorDetail)}",
                System.Net.HttpStatusCode.ServiceUnavailable => 
                    "🔧 Gemini service is temporarily unavailable. Please try again later.",
                System.Net.HttpStatusCode.Forbidden =>
                    "🚫 API access denied. Please verify your API key has the Generative AI API enabled.",
                _ => $"API error ({statusCode}): {(string.IsNullOrEmpty(errorDetail) ? "Please try again later." : errorDetail)}"
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

        #region API Models

        private class GeminiRequest
        {
            public List<GeminiContent> Contents { get; set; } = new();
            public GeminiContent? SystemInstruction { get; set; }
            public GeminiGenerationConfig? GenerationConfig { get; set; }
            public List<GeminiSafetySetting>? SafetySettings { get; set; }
            public List<GeminiTool>? Tools { get; set; }
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

        private class GeminiContent
        {
            public string Role { get; set; } = "user";
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiPart
        {
            public string? Text { get; set; }
            public GeminiFunctionCall? FunctionCall { get; set; }
            public GeminiFunctionResponse? FunctionResponse { get; set; }
        }

        private class GeminiFunctionCall
        {
            public string Name { get; set; } = string.Empty;
            public JsonElement Args { get; set; }
        }

        private class GeminiFunctionResponse
        {
            public string Name { get; set; } = string.Empty;
            public JsonDocument Response { get; set; } = JsonDocument.Parse("{}");
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
        }

        #endregion
    }
}

