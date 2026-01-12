namespace Tracker.Common.Enums
{
    /// <summary>
    /// Types of entities that a note can be linked to.
    /// </summary>
    public enum NoteLinkedEntityType
    {
        /// <summary>No linked entity - standalone note.</summary>
        None = 0,
        
        /// <summary>Linked to a team member.</summary>
        TeamMember = 1,
        
        /// <summary>Linked to a project.</summary>
        Project = 2,
        
        /// <summary>Linked to a 1:1 meeting (legacy - use Meeting).</summary>
        OneOnOne = 3,
        
        /// <summary>Linked to an OKR (Objective).</summary>
        OKR = 4,
        
        /// <summary>Linked to a Key Result (now Target).</summary>
        KeyResult = 5,
        
        /// <summary>Linked to a KPI (now Metric).</summary>
        KPI = 6,
        
        /// <summary>Linked to a task.</summary>
        Task = 7,
        
        /// <summary>Linked to a goal.</summary>
        Goal = 8,
        
        /// <summary>Linked to a feedback entry.</summary>
        Feedback = 9,
        
        /// <summary>Linked to a meeting.</summary>
        Meeting = 10,
        
        /// <summary>Linked to a Target (new name for Key Result).</summary>
        Target = 11,
        
        /// <summary>Linked to a Metric (new name for KPI).</summary>
        Metric = 12
    }
}

