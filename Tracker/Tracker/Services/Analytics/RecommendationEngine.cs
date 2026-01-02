using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Logging;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Generates actionable recommendations based on trajectory analysis.
    /// Provides context-aware suggestions to improve OKR/KPI performance.
    /// </summary>
    public class RecommendationEngine
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(RecommendationEngine));

        #region Singleton

        private static readonly Lazy<RecommendationEngine> _instance = new(() => new RecommendationEngine());
        public static RecommendationEngine Instance => _instance.Value;

        private RecommendationEngine() { }

        #endregion

        #region Models

        /// <summary>
        /// A recommendation with priority and actionable steps.
        /// </summary>
        public class Recommendation
        {
            /// <summary>
            /// Unique identifier for this recommendation type.
            /// </summary>
            public string Id { get; init; } = string.Empty;

            /// <summary>
            /// Short title for the recommendation.
            /// </summary>
            public string Title { get; init; } = string.Empty;

            /// <summary>
            /// Detailed description of the recommendation.
            /// </summary>
            public string Description { get; init; } = string.Empty;

            /// <summary>
            /// Priority level (1 = highest, 5 = lowest).
            /// </summary>
            public int Priority { get; init; } = 3;

            /// <summary>
            /// Category of recommendation.
            /// </summary>
            public RecommendationType Type { get; init; }

            /// <summary>
            /// Icon to display with this recommendation.
            /// </summary>
            public string Icon { get; init; } = "💡";

            /// <summary>
            /// Specific action steps to implement this recommendation.
            /// </summary>
            public IReadOnlyList<string> ActionSteps { get; init; } = Array.Empty<string>();

            /// <summary>
            /// Estimated impact of implementing this recommendation.
            /// </summary>
            public string ExpectedImpact { get; init; } = string.Empty;

            /// <summary>
            /// How urgent is this recommendation.
            /// </summary>
            public Urgency Urgency { get; init; } = Urgency.Medium;
        }

        /// <summary>
        /// Categories of recommendations.
        /// </summary>
        public enum RecommendationType
        {
            VelocityImprovement,
            ScopeAdjustment,
            ResourceAllocation,
            ProcessChange,
            CommunicationAction,
            DataQuality,
            Celebration
        }

        /// <summary>
        /// Urgency levels for recommendations.
        /// </summary>
        public enum Urgency
        {
            Critical,
            High,
            Medium,
            Low,
            Informational
        }

        /// <summary>
        /// Result of recommendation analysis.
        /// </summary>
        public class RecommendationResult
        {
            /// <summary>
            /// Overall summary of the situation.
            /// </summary>
            public string Summary { get; init; } = string.Empty;

            /// <summary>
            /// List of recommendations, ordered by priority.
            /// </summary>
            public IReadOnlyList<Recommendation> Recommendations { get; init; } = Array.Empty<Recommendation>();

            /// <summary>
            /// Primary recommended action.
            /// </summary>
            public Recommendation PrimaryRecommendation => Recommendations.FirstOrDefault();

            /// <summary>
            /// Whether there are any critical recommendations.
            /// </summary>
            public bool HasCriticalRecommendations => Recommendations.Any(r => r.Urgency == Urgency.Critical);

            /// <summary>
            /// Count of recommendations by urgency.
            /// </summary>
            public Dictionary<Urgency, int> RecommendationCounts =>
                Recommendations.GroupBy(r => r.Urgency)
                    .ToDictionary(g => g.Key, g => g.Count());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generate recommendations based on a prediction result.
        /// </summary>
        public RecommendationResult GenerateRecommendations(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            if (prediction == null || !prediction.IsValid)
            {
                return CreateInsufficientDataResult();
            }

            try
            {
                var recommendations = new List<Recommendation>();

                // Analyze different aspects and generate recommendations
                recommendations.AddRange(AnalyzeTrajectory(prediction));
                recommendations.AddRange(AnalyzeTrend(prediction));
                recommendations.AddRange(AnalyzeDataSufficiency(prediction));
                recommendations.AddRange(AnalyzeTimeRemaining(prediction));

                // Sort by priority and urgency
                var sortedRecommendations = recommendations
                    .OrderBy(r => r.Urgency)
                    .ThenBy(r => r.Priority)
                    .ToList();

                // Generate summary
                var summary = GenerateSummary(prediction, sortedRecommendations);

                return new RecommendationResult
                {
                    Summary = summary,
                    Recommendations = sortedRecommendations
                };
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error generating recommendations");
                return CreateErrorResult();
            }
        }

        /// <summary>
        /// Get the single most important recommendation.
        /// </summary>
        public Recommendation GetPrimaryRecommendation(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            var result = GenerateRecommendations(prediction);
            return result.PrimaryRecommendation;
        }

        /// <summary>
        /// Get recommendations filtered by type.
        /// </summary>
        public IReadOnlyList<Recommendation> GetRecommendationsByType(
            PredictiveAnalyticsService.PredictionResult prediction,
            RecommendationType type)
        {
            var result = GenerateRecommendations(prediction);
            return result.Recommendations.Where(r => r.Type == type).ToList();
        }

        #endregion

        #region Analysis Methods

        private IEnumerable<Recommendation> AnalyzeTrajectory(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            var recommendations = new List<Recommendation>();
            var trajectory = prediction.Trajectory;

            if (trajectory == null)
                return recommendations;

            switch (trajectory.Risk)
            {
                case TrajectoryPredictor.RiskLevel.Critical:
                    recommendations.Add(new Recommendation
                    {
                        Id = "critical_intervention",
                        Title = "Immediate Intervention Required",
                        Description = "This goal is critically behind and unlikely to recover without significant changes.",
                        Priority = 1,
                        Type = RecommendationType.ScopeAdjustment,
                        Icon = "🚨",
                        Urgency = Urgency.Critical,
                        ActionSteps = new[]
                        {
                            "Schedule an emergency review meeting within 24 hours",
                            "Identify the top 3 blockers preventing progress",
                            "Consider reducing scope to minimum viable outcome",
                            "Evaluate if additional resources can be allocated",
                            "Prepare contingency plan if target cannot be met"
                        },
                        ExpectedImpact = "Without intervention, target will not be achieved"
                    });
                    break;

                case TrajectoryPredictor.RiskLevel.AtRisk:
                    recommendations.Add(new Recommendation
                    {
                        Id = "at_risk_action",
                        Title = "Action Needed to Stay on Track",
                        Description = "Progress is slower than required to meet the target on time.",
                        Priority = 2,
                        Type = RecommendationType.VelocityImprovement,
                        Icon = "⚠️",
                        Urgency = Urgency.High,
                        ActionSteps = new[]
                        {
                            "Review current blockers and dependencies",
                            "Identify quick wins that can accelerate progress",
                            "Consider shifting priorities to focus on this goal",
                            "Schedule weekly check-ins to monitor progress"
                        },
                        ExpectedImpact = "20-30% velocity increase needed to hit target"
                    });
                    break;

                case TrajectoryPredictor.RiskLevel.OnTrack:
                    if (trajectory.IsOnTrack)
                    {
                        recommendations.Add(new Recommendation
                        {
                            Id = "maintain_pace",
                            Title = "Maintain Current Momentum",
                            Description = "Great progress! Continue at the current pace to achieve the target.",
                            Priority = 5,
                            Type = RecommendationType.Celebration,
                            Icon = "✅",
                            Urgency = Urgency.Informational,
                            ActionSteps = new[]
                            {
                                "Maintain regular check-ins and updates",
                                "Document what's working well for future reference",
                                "Consider if there's opportunity to exceed the target"
                            },
                            ExpectedImpact = "On track to meet or exceed target"
                        });
                    }
                    break;
            }

            // Check for potential early completion
            if (trajectory.PredictedCompletionDate.HasValue &&
                trajectory.TargetDate.HasValue &&
                trajectory.PredictedCompletionDate < trajectory.TargetDate.Value.AddDays(-14))
            {
                recommendations.Add(new Recommendation
                {
                    Id = "early_completion",
                    Title = "Ahead of Schedule",
                    Description = "At current pace, this goal may be completed early.",
                    Priority = 4,
                    Type = RecommendationType.Celebration,
                    Icon = "🎯",
                    Urgency = Urgency.Informational,
                    ActionSteps = new[]
                    {
                        "Consider setting a stretch goal",
                        "Evaluate if resources can help other at-risk goals",
                        "Document the success factors for this goal"
                    },
                    ExpectedImpact = $"May complete ~{(trajectory.TargetDate.Value - trajectory.PredictedCompletionDate.Value).Days} days early"
                });
            }

            return recommendations;
        }

        private IEnumerable<Recommendation> AnalyzeTrend(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            var recommendations = new List<Recommendation>();
            var trend = prediction.Trend;

            if (trend == null)
                return recommendations;

            switch (trend.Direction)
            {
                case TrendAnalyzer.TrendDirection.Declining:
                    recommendations.Add(new Recommendation
                    {
                        Id = "declining_trend",
                        Title = "Declining Progress Trend",
                        Description = "Recent progress has been slower than earlier in the period.",
                        Priority = 2,
                        Type = RecommendationType.ProcessChange,
                        Icon = "📉",
                        Urgency = Urgency.High,
                        ActionSteps = new[]
                        {
                            "Investigate what changed recently",
                            "Check for new blockers or competing priorities",
                            "Review resource availability",
                            "Consider process improvements"
                        },
                        ExpectedImpact = "Addressing the slowdown could restore original pace"
                    });
                    break;

                case TrendAnalyzer.TrendDirection.Improving:
                    if (prediction.Trajectory?.Risk != TrajectoryPredictor.RiskLevel.OnTrack)
                    {
                        recommendations.Add(new Recommendation
                        {
                            Id = "improving_trend",
                            Title = "Positive Momentum Building",
                            Description = "Progress is accelerating - keep up the good work!",
                            Priority = 4,
                            Type = RecommendationType.VelocityImprovement,
                            Icon = "📈",
                            Urgency = Urgency.Low,
                            ActionSteps = new[]
                            {
                                "Identify what's driving the improvement",
                                "Share learnings with the team",
                                "Consider if improvements can be sustained"
                            },
                            ExpectedImpact = "Continued acceleration may bring goal back on track"
                        });
                    }
                    break;

                case TrendAnalyzer.TrendDirection.Insufficient:
                    recommendations.Add(new Recommendation
                    {
                        Id = "stalled_progress",
                        Title = "Progress May Have Stalled",
                        Description = "Not enough recent data points to determine trend.",
                        Priority = 3,
                        Type = RecommendationType.CommunicationAction,
                        Icon = "⏸️",
                        Urgency = Urgency.Medium,
                        ActionSteps = new[]
                        {
                            "Check if progress is being captured regularly",
                            "Follow up with the goal owner",
                            "Ensure updates are being logged"
                        },
                        ExpectedImpact = "Regular updates enable better predictions"
                    });
                    break;
            }

            return recommendations;
        }

        private IEnumerable<Recommendation> AnalyzeDataSufficiency(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            var recommendations = new List<Recommendation>();
            var sufficiency = prediction.DataSufficiency;

            if (sufficiency == null)
                return recommendations;

            if (!sufficiency.IsSufficient)
            {
                recommendations.Add(new Recommendation
                {
                    Id = "insufficient_data",
                    Title = "More Data Needed for Accurate Predictions",
                    Description = sufficiency.Summary,
                    Priority = 3,
                    Type = RecommendationType.DataQuality,
                    Icon = "📊",
                    Urgency = Urgency.Medium,
                    ActionSteps = new[]
                    {
                        "Ensure progress is updated regularly (at least weekly)",
                        "Check that all key results have current values",
                        "Wait a few more data points for reliable predictions"
                    },
                    ExpectedImpact = "Better data leads to more accurate forecasting"
                });
            }
            else if (sufficiency.Confidence == DataSufficiencyChecker.ConfidenceLevel.Low ||
                     sufficiency.Confidence == DataSufficiencyChecker.ConfidenceLevel.VeryLow)
            {
                recommendations.Add(new Recommendation
                {
                    Id = "low_confidence",
                    Title = "Predictions Have Higher Uncertainty",
                    Description = "Data variance is high, making predictions less reliable.",
                    Priority = 4,
                    Type = RecommendationType.DataQuality,
                    Icon = "🎲",
                    Urgency = Urgency.Low,
                    ActionSteps = new[]
                    {
                        "Review data for any anomalies or outliers",
                        "Ensure consistent measurement methodology",
                        "Consider if external factors are causing variance"
                    },
                    ExpectedImpact = "More consistent data improves prediction reliability"
                });
            }

            return recommendations;
        }

        private IEnumerable<Recommendation> AnalyzeTimeRemaining(
            PredictiveAnalyticsService.PredictionResult prediction)
        {
            var recommendations = new List<Recommendation>();
            var trajectory = prediction.Trajectory;

            if (trajectory?.TargetDate == null || !trajectory.TargetDate.HasValue)
                return recommendations;

            var daysRemaining = (trajectory.TargetDate.Value - DateTime.Today).Days;

            // Last week warning
            if (daysRemaining <= 7 && daysRemaining > 0 &&
                trajectory?.Risk != TrajectoryPredictor.RiskLevel.OnTrack)
            {
                recommendations.Add(new Recommendation
                {
                    Id = "final_week",
                    Title = "Final Week - Decision Time",
                    Description = "Less than a week remains to achieve this goal.",
                    Priority = 1,
                    Type = RecommendationType.ScopeAdjustment,
                    Icon = "⏰",
                    Urgency = Urgency.Critical,
                    ActionSteps = new[]
                    {
                        "Make a go/no-go decision on hitting the target",
                        "If proceeding, focus exclusively on this goal",
                        "Consider partial credit or adjusted targets",
                        "Prepare retrospective notes"
                    },
                    ExpectedImpact = "Final opportunity for course correction"
                });
            }
            // Two weeks warning
            else if (daysRemaining <= 14 && daysRemaining > 7 &&
                     trajectory?.Risk == TrajectoryPredictor.RiskLevel.AtRisk)
            {
                recommendations.Add(new Recommendation
                {
                    Id = "two_weeks_warning",
                    Title = "Two Weeks Remaining",
                    Description = "Limited time left - prioritization is key.",
                    Priority = 2,
                    Type = RecommendationType.ResourceAllocation,
                    Icon = "📅",
                    Urgency = Urgency.High,
                    ActionSteps = new[]
                    {
                        "Clear calendar for focused work time",
                        "Deprioritize non-essential tasks",
                        "Request help if needed",
                        "Break remaining work into daily targets"
                    },
                    ExpectedImpact = "Focused effort can still achieve the target"
                });
            }
            // Deadline passed
            else if (daysRemaining < 0)
            {
                recommendations.Add(new Recommendation
                {
                    Id = "past_deadline",
                    Title = "Deadline Has Passed",
                    Description = "The target date has passed. Time for a retrospective.",
                    Priority = 1,
                    Type = RecommendationType.CommunicationAction,
                    Icon = "📋",
                    Urgency = Urgency.High,
                    ActionSteps = new[]
                    {
                        "Document final outcome and learnings",
                        "Conduct a brief retrospective",
                        "Decide whether to extend or close the goal",
                        "Apply learnings to future goals"
                    },
                    ExpectedImpact = "Closure and learning opportunity"
                });
            }

            return recommendations;
        }

        private string GenerateSummary(
            PredictiveAnalyticsService.PredictionResult prediction,
            IReadOnlyList<Recommendation> recommendations)
        {
            var criticalCount = recommendations.Count(r => r.Urgency == Urgency.Critical);
            var highCount = recommendations.Count(r => r.Urgency == Urgency.High);
            var trajectory = prediction.Trajectory;

            if (criticalCount > 0)
            {
                return $"🚨 {criticalCount} critical issue(s) require immediate attention. " +
                       $"Current trajectory: {trajectory?.Risk.ToString() ?? "Unknown"}";
            }
            else if (highCount > 0)
            {
                return $"⚠️ {highCount} high-priority recommendation(s). " +
                       $"Action needed to stay on track.";
            }
            else if (trajectory?.Risk == TrajectoryPredictor.RiskLevel.OnTrack)
            {
                return "✅ On track! Continue current pace to achieve the target.";
            }
            else
            {
                return $"📊 {recommendations.Count} suggestion(s) to improve outcomes.";
            }
        }

        private RecommendationResult CreateInsufficientDataResult()
        {
            return new RecommendationResult
            {
                Summary = "Not enough data to generate recommendations",
                Recommendations = new[]
                {
                    new Recommendation
                    {
                        Id = "need_more_data",
                        Title = "Gather More Data",
                        Description = "Continue tracking progress to enable recommendations.",
                        Priority = 3,
                        Type = RecommendationType.DataQuality,
                        Icon = "📊",
                        Urgency = Urgency.Medium,
                        ActionSteps = new[]
                        {
                            "Update progress values regularly",
                            "Check back after a few more data points"
                        },
                        ExpectedImpact = "Recommendations will be available soon"
                    }
                }
            };
        }

        private RecommendationResult CreateErrorResult()
        {
            return new RecommendationResult
            {
                Summary = "Unable to generate recommendations at this time",
                Recommendations = Array.Empty<Recommendation>()
            };
        }

        #endregion
    }
}
