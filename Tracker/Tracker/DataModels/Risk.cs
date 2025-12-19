using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A project risk - something that could go wrong.
    /// Simplified model with single severity level.
    /// </summary>
    public class Risk : AuditableEntity
    {
        public int ID { get; set; } = 0;
        public int ProjectId { get; set; } = 0;

        /// <summary>
        /// Short name for the risk.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the risk.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Overall severity: Low, Medium, High, Critical.
        /// </summary>
        public RiskLevelEnum Severity { get; set; } = RiskLevelEnum.Medium;
        
        /// <summary>
        /// What we're doing to address it.
        /// </summary>
        public string MitigationStrategy { get; set; } = string.Empty;
        
        /// <summary>
        /// When the risk was identified.
        /// </summary>
        public DateTime? IdentifiedDate { get; set; }
        
        /// <summary>
        /// Whether the risk has been mitigated/resolved.
        /// </summary>
        public bool IsMitigated { get; set; }
    }
}
