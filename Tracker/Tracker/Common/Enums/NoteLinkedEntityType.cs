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
        
        /// <summary>Linked to a 1:1 meeting.</summary>
        OneOnOne = 3,
        
        /// <summary>Linked to an OKR (Objective).</summary>
        OKR = 4,
        
        /// <summary>Linked to a Key Result.</summary>
        KeyResult = 5,
        
        /// <summary>Linked to a KPI.</summary>
        KPI = 6,
        
        /// <summary>Linked to a task.</summary>
        Task = 7,
        
        /// <summary>Linked to a goal.</summary>
        Goal = 8,
        
        /// <summary>Linked to a feedback entry.</summary>
        Feedback = 9
    }
}

