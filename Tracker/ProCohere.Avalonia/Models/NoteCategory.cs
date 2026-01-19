namespace ProCohere.Avalonia.Models;

/// <summary>
/// Predefined note categories for organization.
/// Values match database constraints.
/// </summary>
public static class NoteCategory
{
    public const string General = "general";
    public const string Observation = "observation";
    public const string Idea = "idea";
    public const string Feedback = "feedback";
    public const string Decision = "decision";
    public const string ActionItem = "action_item";
    public const string Question = "question";
    public const string Risk = "risk";
    public const string Success = "success";
    public const string Learning = "learning";

    /// <summary>
    /// All available categories for dropdown/selection.
    /// </summary>
    public static readonly string[] All =
    {
        General, Observation, Idea, Feedback, Decision,
        ActionItem, Question, Risk, Success, Learning
    };

    /// <summary>
    /// Gets a display-friendly name for a category.
    /// </summary>
    public static string GetDisplayName(string? category) => category switch
    {
        General => "General",
        Observation => "Observation",
        Idea => "Idea",
        Feedback => "Feedback",
        Decision => "Decision",
        ActionItem => "Action Item",
        Question => "Question",
        Risk => "Risk",
        Success => "Success",
        Learning => "Learning",
        _ => category ?? "None"
    };
}
