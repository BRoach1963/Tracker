using System;
using System.Collections.Generic;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Simulates "what-if" scenarios for OKRs, KPIs, and other trackable entities.
    /// Allows users to explore how changes in velocity would affect outcomes.
    /// </summary>
    public class WhatIfSimulator
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(WhatIfSimulator));

        #region Singleton

        private static readonly Lazy<WhatIfSimulator> _instance = new(() => new WhatIfSimulator());
        public static WhatIfSimulator Instance => _instance.Value;

        private WhatIfSimulator() { }

        #endregion

        #region Models

        /// <summary>
        /// Represents a what-if scenario with modified parameters.
        /// </summary>
        public class WhatIfScenario
        {
            /// <summary>
            /// Name of the scenario (e.g., "Increase velocity 20%").
            /// </summary>
            public string Name { get; init; } = string.Empty;

            /// <summary>
            /// Description of what this scenario represents.
            /// </summary>
            public string Description { get; init; } = string.Empty;

            /// <summary>
            /// Velocity multiplier (1.0 = no change, 1.2 = 20% faster, 0.8 = 20% slower).
            /// </summary>
            public double VelocityMultiplier { get; init; } = 1.0;

            /// <summary>
            /// Additional daily progress to add (absolute value).
            /// </summary>
            public double AdditionalDailyProgress { get; init; } = 0;

            /// <summary>
            /// Days to delay before intervention takes effect.
            /// </summary>
            public int DelayDays { get; init; } = 0;
        }

        /// <summary>
        /// Result of running a what-if simulation.
        /// </summary>
        public class WhatIfResult
        {
            /// <summary>
            /// The scenario that was simulated.
            /// </summary>
            public WhatIfScenario Scenario { get; init; } = new();

            /// <summary>
            /// Original (baseline) prediction.
            /// </summary>
            public BaselineMetrics Baseline { get; init; } = new();

            /// <summary>
            /// Simulated outcome with the scenario applied.
            /// </summary>
            public SimulatedOutcome Outcome { get; init; } = new();

            /// <summary>
            /// Impact analysis comparing baseline to outcome.
            /// </summary>
            public ImpactAnalysis Impact { get; init; } = new();

            /// <summary>
            /// Whether this scenario allows hitting the target on time.
            /// </summary>
            public bool WillHitTarget => Outcome.WillHitTarget;

            /// <summary>
            /// Human-readable summary of the simulation result.
            /// </summary>
            public string Summary { get; init; } = string.Empty;
        }

        /// <summary>
        /// Baseline metrics before applying the scenario.
        /// </summary>
        public class BaselineMetrics
        {
            public double CurrentProgress { get; init; }
            public double CurrentVelocity { get; init; }
            public double ProjectedFinalProgress { get; init; }
            public DateTime? ProjectedCompletionDate { get; init; }
            public int DaysRemaining { get; init; }
            public TrajectoryPredictor.RiskLevel RiskLevel { get; init; }
        }

        /// <summary>
        /// Simulated outcome after applying the scenario.
        /// </summary>
        public class SimulatedOutcome
        {
            public double NewVelocity { get; init; }
            public double ProjectedFinalProgress { get; init; }
            public DateTime? ProjectedCompletionDate { get; init; }
            public bool WillHitTarget { get; init; }
            public int? DaysToTarget { get; init; }
            public TrajectoryPredictor.RiskLevel NewRiskLevel { get; init; }
        }

        /// <summary>
        /// Analysis of the impact of the scenario.
        /// </summary>
        public class ImpactAnalysis
        {
            public double VelocityChange { get; init; }
            public double VelocityChangePercent { get; init; }
            public double ProgressImprovement { get; init; }
            public int? DaysSaved { get; init; }
            public bool RiskLevelImproved { get; init; }
            public string ImpactDescription { get; init; } = string.Empty;
        }

        #endregion

        #region Predefined Scenarios

        /// <summary>
        /// Get a set of predefined scenarios for quick simulation.
        /// </summary>
        public IReadOnlyList<WhatIfScenario> GetPredefinedScenarios()
        {
            return new List<WhatIfScenario>
            {
                new WhatIfScenario
                {
                    Name = "10% Velocity Increase",
                    Description = "Team increases pace by 10%",
                    VelocityMultiplier = 1.10
                },
                new WhatIfScenario
                {
                    Name = "20% Velocity Increase",
                    Description = "Team increases pace by 20% (requires focus)",
                    VelocityMultiplier = 1.20
                },
                new WhatIfScenario
                {
                    Name = "50% Velocity Increase",
                    Description = "Major intervention - double down on this goal",
                    VelocityMultiplier = 1.50
                },
                new WhatIfScenario
                {
                    Name = "Add Resource",
                    Description = "Add one team member (estimated +1% daily progress)",
                    AdditionalDailyProgress = 1.0
                },
                new WhatIfScenario
                {
                    Name = "Sprint Mode",
                    Description = "Intensive focus for remaining time (+2% daily)",
                    AdditionalDailyProgress = 2.0
                },
                new WhatIfScenario
                {
                    Name = "Delayed Start (1 Week)",
                    Description = "Intervention starts in 1 week",
                    VelocityMultiplier = 1.30,
                    DelayDays = 7
                },
                new WhatIfScenario
                {
                    Name = "Current Pace",
                    Description = "Continue at current velocity",
                    VelocityMultiplier = 1.0
                },
                new WhatIfScenario
                {
                    Name = "10% Slowdown",
                    Description = "Velocity decreases due to distractions",
                    VelocityMultiplier = 0.90
                }
            };
        }

        #endregion

        #region Simulation Methods

        /// <summary>
        /// Run a what-if simulation for a given prediction and scenario.
        /// </summary>
        public WhatIfResult Simulate(
            PredictiveAnalyticsService.PredictionResult prediction,
            WhatIfScenario scenario)
        {
            if (prediction == null || !prediction.IsValid)
            {
                _logger.Warn("Cannot simulate with invalid prediction");
                return CreateInvalidResult(scenario, "Invalid or insufficient prediction data");
            }

            try
            {
                var baseline = ExtractBaseline(prediction);
                var outcome = CalculateOutcome(baseline, scenario, prediction.Trajectory?.TargetDate);
                var impact = AnalyzeImpact(baseline, outcome, scenario);
                var summary = GenerateSummary(scenario, baseline, outcome, impact);

                return new WhatIfResult
                {
                    Scenario = scenario,
                    Baseline = baseline,
                    Outcome = outcome,
                    Impact = impact,
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error running what-if simulation");
                return CreateInvalidResult(scenario, $"Simulation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Run multiple scenarios and return all results.
        /// </summary>
        public IReadOnlyList<WhatIfResult> SimulateMultiple(
            PredictiveAnalyticsService.PredictionResult prediction,
            IEnumerable<WhatIfScenario> scenarios)
        {
            var results = new List<WhatIfResult>();

            foreach (var scenario in scenarios)
            {
                results.Add(Simulate(prediction, scenario));
            }

            return results;
        }

        /// <summary>
        /// Calculate the minimum velocity multiplier needed to hit the target on time.
        /// </summary>
        public WhatIfScenario CalculateRequiredVelocity(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            if (prediction == null || !prediction.IsValid || prediction.Trajectory == null)
            {
                return new WhatIfScenario
                {
                    Name = "Unable to Calculate",
                    Description = "Insufficient data for calculation",
                    VelocityMultiplier = 1.0
                };
            }

            var baseline = ExtractBaseline(prediction);
            
            if (baseline.CurrentVelocity <= 0)
            {
                return new WhatIfScenario
                {
                    Name = "Unable to Calculate",
                    Description = "Current velocity is zero - cannot calculate multiplier",
                    VelocityMultiplier = double.MaxValue
                };
            }

            var targetProgress = 100.0; // Assuming 100% completion
            var remainingProgress = targetProgress - baseline.CurrentProgress;

            if (remainingProgress <= 0)
            {
                return new WhatIfScenario
                {
                    Name = "Already On Track",
                    Description = "Target already achieved",
                    VelocityMultiplier = 1.0
                };
            }

            var requiredVelocity = remainingProgress / Math.Max(1, baseline.DaysRemaining);
            var multiplier = requiredVelocity / baseline.CurrentVelocity;

            return new WhatIfScenario
            {
                Name = $"Required: {multiplier:P0} Velocity",
                Description = $"Need {multiplier:P0} of current pace to hit target on time",
                VelocityMultiplier = multiplier
            };
        }

        /// <summary>
        /// Create a custom scenario with a specific velocity multiplier.
        /// </summary>
        public WhatIfScenario CreateCustomScenario(double velocityMultiplier, string name = null)
        {
            var percentChange = (velocityMultiplier - 1.0) * 100;
            var direction = percentChange >= 0 ? "increase" : "decrease";

            return new WhatIfScenario
            {
                Name = name ?? $"{Math.Abs(percentChange):F0}% Velocity {(percentChange >= 0 ? "Increase" : "Decrease")}",
                Description = $"Velocity {direction} by {Math.Abs(percentChange):F0}%",
                VelocityMultiplier = velocityMultiplier
            };
        }

        #endregion

        #region Private Methods

        private BaselineMetrics ExtractBaseline(PredictiveAnalyticsService.PredictionResult prediction)
        {
            var trajectory = prediction.Trajectory;
            var trend = prediction.Trend;
            var trajectoryPoints = prediction.TrajectoryPoints;

            // Calculate current velocity from trajectory points
            double currentVelocity = 0;
            if (trajectoryPoints != null && trajectoryPoints.Count >= 2)
            {
                var first = trajectoryPoints[0];
                var last = trajectoryPoints[^1];
                var days = (last.Date - first.Date).TotalDays;
                if (days > 0)
                {
                    currentVelocity = (last.ProjectedProgress - first.ProjectedProgress) / days;
                }
            }
            else if (trend != null)
            {
                // Fallback to using slope from trend analysis
                currentVelocity = trend.Slope;
            }

            // Calculate days remaining from trajectory target date
            int daysRemaining = 0;
            if (trajectory?.TargetDate.HasValue == true)
            {
                daysRemaining = Math.Max(0, (trajectory.TargetDate.Value - DateTime.Today).Days);
            }

            // Get current progress from trajectory
            double currentProgress = trajectory?.CurrentProgress ?? 0;

            // Calculate projected final progress
            double projectedFinal = currentProgress + (currentVelocity * daysRemaining);

            return new BaselineMetrics
            {
                CurrentProgress = currentProgress,
                CurrentVelocity = currentVelocity,
                ProjectedFinalProgress = projectedFinal,
                ProjectedCompletionDate = trajectory?.PredictedCompletionDate,
                DaysRemaining = daysRemaining,
                RiskLevel = trajectory?.Risk ?? TrajectoryPredictor.RiskLevel.Unknown
            };
        }

        private SimulatedOutcome CalculateOutcome(
            BaselineMetrics baseline,
            WhatIfScenario scenario,
            DateTime? targetDate)
        {
            // Calculate new velocity
            var newVelocity = (baseline.CurrentVelocity * scenario.VelocityMultiplier)
                              + scenario.AdditionalDailyProgress;

            // Account for delay
            var effectiveDays = Math.Max(0, baseline.DaysRemaining - scenario.DelayDays);
            
            // Progress during delay (at old velocity)
            var delayProgress = baseline.CurrentVelocity * scenario.DelayDays;
            
            // Progress after intervention
            var interventionProgress = newVelocity * effectiveDays;

            // Total projected progress
            var projectedFinal = baseline.CurrentProgress + delayProgress + interventionProgress;

            // Calculate days to target
            var remainingToTarget = 100.0 - baseline.CurrentProgress;
            int? daysToTarget = null;
            DateTime? completionDate = null;

            if (newVelocity > 0 && remainingToTarget > 0)
            {
                // Account for delay in calculation
                var daysNeeded = (int)Math.Ceiling(remainingToTarget / newVelocity);
                daysToTarget = scenario.DelayDays + daysNeeded;
                completionDate = DateTime.Today.AddDays(daysToTarget.Value);
            }

            // Determine new risk level
            var willHitTarget = projectedFinal >= 100.0 && 
                               (targetDate == null || completionDate <= targetDate);
            
            var newRiskLevel = DetermineRiskLevel(projectedFinal, willHitTarget, effectiveDays);

            return new SimulatedOutcome
            {
                NewVelocity = newVelocity,
                ProjectedFinalProgress = Math.Min(projectedFinal, 100.0),
                ProjectedCompletionDate = completionDate,
                WillHitTarget = willHitTarget,
                DaysToTarget = daysToTarget,
                NewRiskLevel = newRiskLevel
            };
        }

        private TrajectoryPredictor.RiskLevel DetermineRiskLevel(
            double projectedFinal,
            bool willHitTarget,
            int daysRemaining)
        {
            if (willHitTarget)
                return TrajectoryPredictor.RiskLevel.OnTrack;

            if (projectedFinal >= 90.0)
                return TrajectoryPredictor.RiskLevel.OnTrack;

            if (projectedFinal >= 70.0)
                return TrajectoryPredictor.RiskLevel.AtRisk;

            if (projectedFinal >= 50.0 || daysRemaining > 14)
                return TrajectoryPredictor.RiskLevel.AtRisk;

            return TrajectoryPredictor.RiskLevel.Critical;
        }

        private ImpactAnalysis AnalyzeImpact(
            BaselineMetrics baseline,
            SimulatedOutcome outcome,
            WhatIfScenario scenario)
        {
            var velocityChange = outcome.NewVelocity - baseline.CurrentVelocity;
            var velocityChangePercent = baseline.CurrentVelocity > 0
                ? (velocityChange / baseline.CurrentVelocity) * 100
                : 0;

            var progressImprovement = outcome.ProjectedFinalProgress - baseline.ProjectedFinalProgress;

            int? daysSaved = null;
            if (baseline.ProjectedCompletionDate.HasValue && outcome.ProjectedCompletionDate.HasValue)
            {
                daysSaved = (baseline.ProjectedCompletionDate.Value - outcome.ProjectedCompletionDate.Value).Days;
            }

            var riskImproved = (int)outcome.NewRiskLevel < (int)baseline.RiskLevel;

            var impactDescription = GenerateImpactDescription(
                velocityChangePercent, progressImprovement, daysSaved, riskImproved, outcome.WillHitTarget);

            return new ImpactAnalysis
            {
                VelocityChange = velocityChange,
                VelocityChangePercent = velocityChangePercent,
                ProgressImprovement = progressImprovement,
                DaysSaved = daysSaved,
                RiskLevelImproved = riskImproved,
                ImpactDescription = impactDescription
            };
        }

        private string GenerateImpactDescription(
            double velocityChangePercent,
            double progressImprovement,
            int? daysSaved,
            bool riskImproved,
            bool willHitTarget)
        {
            var parts = new List<string>();

            if (willHitTarget)
            {
                parts.Add("✅ Will hit target on time");
            }
            else
            {
                parts.Add("⚠️ Still won't hit target");
            }

            if (Math.Abs(progressImprovement) > 0.5)
            {
                var direction = progressImprovement > 0 ? "+" : "";
                parts.Add($"Progress: {direction}{progressImprovement:F1}%");
            }

            if (daysSaved.HasValue && Math.Abs(daysSaved.Value) > 0)
            {
                if (daysSaved.Value > 0)
                    parts.Add($"⏱️ {daysSaved.Value} days faster");
                else
                    parts.Add($"⏱️ {Math.Abs(daysSaved.Value)} days slower");
            }

            if (riskImproved)
            {
                parts.Add("📉 Risk level improved");
            }

            return string.Join(" | ", parts);
        }

        private string GenerateSummary(
            WhatIfScenario scenario,
            BaselineMetrics baseline,
            SimulatedOutcome outcome,
            ImpactAnalysis impact)
        {
            if (outcome.WillHitTarget)
            {
                return $"Applying '{scenario.Name}' would allow reaching the target" +
                       (outcome.ProjectedCompletionDate.HasValue
                           ? $" by {outcome.ProjectedCompletionDate.Value:MMM d, yyyy}"
                           : "") +
                       $" with {outcome.ProjectedFinalProgress:F0}% completion.";
            }
            else
            {
                return $"Applying '{scenario.Name}' would improve projected completion to " +
                       $"{outcome.ProjectedFinalProgress:F0}% (from {baseline.ProjectedFinalProgress:F0}%), " +
                       "but still wouldn't hit the target.";
            }
        }

        private WhatIfResult CreateInvalidResult(WhatIfScenario scenario, string reason)
        {
            return new WhatIfResult
            {
                Scenario = scenario,
                Baseline = new BaselineMetrics(),
                Outcome = new SimulatedOutcome(),
                Impact = new ImpactAnalysis { ImpactDescription = reason },
                Summary = reason
            };
        }

        #endregion
    }
}
