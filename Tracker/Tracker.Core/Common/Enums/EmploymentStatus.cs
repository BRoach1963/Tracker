namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Employment status for team members.
    /// Maps to PostgreSQL employment_status enum.
    /// </summary>
    public enum EmploymentStatus
    {
        /// <summary>
        /// Currently employed and working.
        /// </summary>
        Active,

        /// <summary>
        /// On leave (medical, parental, sabbatical, etc.).
        /// </summary>
        OnLeave,

        /// <summary>
        /// Employment terminated.
        /// </summary>
        Terminated,

        /// <summary>
        /// External contractor (not full employee).
        /// </summary>
        Contractor
    }
}
