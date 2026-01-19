using System;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal lifecycle states - tracks the evolution of a goal.
/// Philosophy: Goals evolve naturally; lifecycle captures that journey.
/// </summary>
public enum GoalLifecycle
{
    /// <summary>
    /// Goal matters right now.
    /// Actively being worked on and discussed.
    /// </summary>
    Active,

    /// <summary>
    /// Meaning or scope is changing.
    /// The goal is being refined or pivoted.
    /// </summary>
    Evolving,

    /// <summary>
    /// Matters but not currently.
    /// Temporarily deprioritized, will return.
    /// </summary>
    Paused,

    /// <summary>
    /// Replaced by new goals.
    /// Terminal state - links to replacement goal.
    /// </summary>
    Superseded,

    /// <summary>
    /// No longer matters.
    /// Terminal state - goal has run its course.
    /// </summary>
    Retired
}

/// <summary>
/// Extension methods for GoalLifecycle.
/// </summary>
public static class GoalLifecycleExtensions
{
    /// <summary>
    /// Gets the display name for a lifecycle state.
    /// </summary>
    public static string ToDisplayName(this GoalLifecycle lifecycle) => lifecycle switch
    {
        GoalLifecycle.Active => "Active",
        GoalLifecycle.Evolving => "Evolving",
        GoalLifecycle.Paused => "Paused",
        GoalLifecycle.Superseded => "Superseded",
        GoalLifecycle.Retired => "Retired",
        _ => lifecycle.ToString()
    };

    /// <summary>
    /// Gets a description for the lifecycle state.
    /// </summary>
    public static string ToDescription(this GoalLifecycle lifecycle) => lifecycle switch
    {
        GoalLifecycle.Active => "Goal is actively being pursued",
        GoalLifecycle.Evolving => "Goal's scope or meaning is changing",
        GoalLifecycle.Paused => "Temporarily deprioritized",
        GoalLifecycle.Superseded => "Replaced by a new goal",
        GoalLifecycle.Retired => "No longer relevant",
        _ => string.Empty
    };

    /// <summary>
    /// Checks if this is a terminal lifecycle state.
    /// </summary>
    public static bool IsTerminal(this GoalLifecycle lifecycle) => lifecycle switch
    {
        GoalLifecycle.Superseded => true,
        GoalLifecycle.Retired => true,
        _ => false
    };

    /// <summary>
    /// Checks if the goal is currently actionable.
    /// </summary>
    public static bool IsActionable(this GoalLifecycle lifecycle) => lifecycle switch
    {
        GoalLifecycle.Active => true,
        GoalLifecycle.Evolving => true,
        _ => false
    };

    /// <summary>
    /// Gets a reflection prompt when changing lifecycle.
    /// </summary>
    public static string GetReflectionPrompt(this GoalLifecycle lifecycle) => lifecycle switch
    {
        GoalLifecycle.Active => "What makes this goal ready for active work?",
        GoalLifecycle.Evolving => "How is this goal's meaning or scope changing?",
        GoalLifecycle.Paused => "Why is this being paused, and when might it resume?",
        GoalLifecycle.Superseded => "What new goal is replacing this one?",
        GoalLifecycle.Retired => "What led to retiring this goal?",
        _ => "What has changed?"
    };

    /// <summary>
    /// Parses a string to GoalLifecycle.
    /// </summary>
    public static GoalLifecycle ParseGoalLifecycle(string? value) => value?.ToLower() switch
    {
        "active" => GoalLifecycle.Active,
        "evolving" => GoalLifecycle.Evolving,
        "paused" => GoalLifecycle.Paused,
        "superseded" => GoalLifecycle.Superseded,
        "retired" => GoalLifecycle.Retired,
        _ => GoalLifecycle.Active // Default
    };
}
