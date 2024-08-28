using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class Risk
    {
        public int ID { get; set; } = 0;

        public int ProjectId { get; set; } = 0;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevelEnum RiskLevel { get; set; }  

        public RiskLevelEnum Likelihood { get; set; }

        public RiskLevelEnum Impact { get; set; }
        public string MitigationStrategy { get; set; } = string.Empty;
        public DateTime? IdentifiedDate { get; set; }
        public bool IsMitigated { get; set; }
    }
}
