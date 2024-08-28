using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tracker.DataModels
{
    public class ProjectDependency
    {
        public int ID { get; set; } = 0;

        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; } = 0;
        public int DependentProjectID { get; set; } = 0;
        public int RequiredProjectID { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpectedCompletionDate { get; set; }
    }
}
