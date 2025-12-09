using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class KeyPerformanceIndicator : AuditableEntity
    {
        public int KpiId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; } = double.NaN;
        public double TargetValue { get; set; } = double.NaN;
        public TeamMember Owner { get; set; } = new();
        public DateTime LastUpdated { get; set; }

        public TargetDirectionEnum TargetDirection { get; set; } = TargetDirectionEnum.GreaterOrEqual;

        public double OnTargetThresholdPercentage { get; set; } = 5.0; // Default is 5%
        public double OffTargetThresholdPercentage { get; set; } = 10.0; // Default is 10%

        public double OnTargetThresholdAbsolute { get; set; } = 0.5;  // Default is 0.5 units
        public double OffTargetThresholdAbsolute { get; set; } = 1.0; // Default is 1.0 units

        public int OkrId { get; set; }

        public KpiStatusEnum Status
        {
            get
            {
                var deviationPercentage = (Value - TargetValue) / TargetValue * 100;
                var deviationAbsolute = Math.Abs(Value - TargetValue);

                bool isOnTarget = deviationPercentage <= OnTargetThresholdPercentage || deviationAbsolute <= OnTargetThresholdAbsolute;
                bool isOffTarget = deviationPercentage > OffTargetThresholdPercentage && deviationAbsolute > OffTargetThresholdAbsolute;

                if (TargetDirection == TargetDirectionEnum.GreaterOrEqual)
                {
                    if (Value >= TargetValue || isOnTarget)
                    {
                        return KpiStatusEnum.OnTarget; // Green
                    }
                    else if (isOffTarget)
                    {
                        return KpiStatusEnum.OffTarget; // Red
                    }
                    else
                    {
                        return KpiStatusEnum.CloseToTarget; // Amber
                    }
                }
                else // TargetDirection.LessOrEqual
                {
                    if (Value <= TargetValue || isOnTarget)
                    {
                        return KpiStatusEnum.OnTarget; // Green
                    }
                    else if (isOffTarget)
                    {
                        return KpiStatusEnum.OffTarget; // Red
                    }
                    else
                    {
                        return KpiStatusEnum.CloseToTarget; // Amber
                    }
                }
            }
        }
    }
}
