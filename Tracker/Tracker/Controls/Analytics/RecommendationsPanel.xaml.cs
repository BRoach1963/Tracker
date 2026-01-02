using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.Services.Analytics;

namespace Tracker.Controls.Analytics
{
    /// <summary>
    /// Displays recommendations based on trajectory analysis.
    /// Shows prioritized, actionable suggestions for improving outcomes.
    /// </summary>
    public partial class RecommendationsPanel : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty PredictionProperty =
            DependencyProperty.Register(
                nameof(Prediction),
                typeof(PredictiveAnalyticsService.PredictionResult),
                typeof(RecommendationsPanel),
                new PropertyMetadata(null, OnPredictionChanged));

        /// <summary>
        /// The prediction result to generate recommendations for.
        /// </summary>
        public PredictiveAnalyticsService.PredictionResult Prediction
        {
            get => (PredictiveAnalyticsService.PredictionResult)GetValue(PredictionProperty);
            set => SetValue(PredictionProperty, value);
        }

        public static readonly DependencyProperty MaxRecommendationsProperty =
            DependencyProperty.Register(
                nameof(MaxRecommendations),
                typeof(int),
                typeof(RecommendationsPanel),
                new PropertyMetadata(5, OnPredictionChanged));

        /// <summary>
        /// Maximum number of recommendations to display.
        /// </summary>
        public int MaxRecommendations
        {
            get => (int)GetValue(MaxRecommendationsProperty);
            set => SetValue(MaxRecommendationsProperty, value);
        }

        public static readonly DependencyProperty ShowActionStepsProperty =
            DependencyProperty.Register(
                nameof(ShowActionSteps),
                typeof(bool),
                typeof(RecommendationsPanel),
                new PropertyMetadata(true, OnPredictionChanged));

        /// <summary>
        /// Whether to show expandable action steps for each recommendation.
        /// </summary>
        public bool ShowActionSteps
        {
            get => (bool)GetValue(ShowActionStepsProperty);
            set => SetValue(ShowActionStepsProperty, value);
        }

        #endregion

        private readonly RecommendationEngine _engine = RecommendationEngine.Instance;

        public RecommendationsPanel()
        {
            InitializeComponent();
        }

        #region Property Changed Handlers

        private static void OnPredictionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (RecommendationsPanel)d;
            control.UpdateDisplay();
        }

        #endregion

        #region Update Methods

        private void UpdateDisplay()
        {
            if (Prediction == null || !Prediction.IsValid)
            {
                ShowEmptyState("Not enough data for recommendations");
                return;
            }

            try
            {
                var result = _engine.GenerateRecommendations(Prediction);
                
                if (result.Recommendations.Count == 0)
                {
                    ShowEmptyState("No recommendations at this time");
                    return;
                }

                EmptyStateText.Visibility = Visibility.Collapsed;
                SummaryText.Text = result.Summary;

                UpdateBadges(result);
                UpdateRecommendationCards(result);
            }
            catch (Exception ex)
            {
                ShowEmptyState($"Error: {ex.Message}");
            }
        }

        private void ShowEmptyState(string message)
        {
            RecommendationsList.Children.Clear();
            EmptyStateText.Text = message;
            EmptyStateText.Visibility = Visibility.Visible;
            SummaryText.Text = "";
            BadgesPanel.Children.Clear();
        }

        private void UpdateBadges(RecommendationEngine.RecommendationResult result)
        {
            BadgesPanel.Children.Clear();

            foreach (var kvp in result.RecommendationCounts.OrderBy(k => k.Key))
            {
                var badge = CreateUrgencyBadge(kvp.Key, kvp.Value);
                BadgesPanel.Children.Add(badge);
            }
        }

        private Border CreateUrgencyBadge(RecommendationEngine.Urgency urgency, int count)
        {
            var brush = GetUrgencyBrush(urgency);
            
            var badge = new Border
            {
                Background = brush,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var text = new TextBlock
            {
                Text = $"{count} {urgency}",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };

            badge.Child = text;
            return badge;
        }

        private void UpdateRecommendationCards(RecommendationEngine.RecommendationResult result)
        {
            RecommendationsList.Children.Clear();
            RecommendationsList.Children.Add(EmptyStateText);

            var recommendations = result.Recommendations.Take(MaxRecommendations);

            foreach (var rec in recommendations)
            {
                var card = CreateRecommendationCard(rec);
                RecommendationsList.Children.Add(card);
            }
        }

        private Border CreateRecommendationCard(RecommendationEngine.Recommendation recommendation)
        {
            var borderBrush = GetUrgencyBrush(recommendation.Urgency);

            var card = new Border
            {
                Background = (Brush)FindResource("CardBackground"),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var contentPanel = new StackPanel();

            // Header row with icon and title
            var headerPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            
            var iconText = new TextBlock
            {
                Text = recommendation.Icon,
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(iconText);

            var titleText = new TextBlock
            {
                Text = recommendation.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("PrimaryText"),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            headerPanel.Children.Add(titleText);

            // Urgency badge
            var urgencyBadge = new Border
            {
                Background = borderBrush,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            urgencyBadge.Child = new TextBlock
            {
                Text = recommendation.Urgency.ToString(),
                FontSize = 10,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(urgencyBadge);

            contentPanel.Children.Add(headerPanel);

            // Description
            var descText = new TextBlock
            {
                Text = recommendation.Description,
                FontSize = 12,
                Foreground = (Brush)FindResource("SecondaryText"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            contentPanel.Children.Add(descText);

            // Expected Impact
            if (!string.IsNullOrEmpty(recommendation.ExpectedImpact))
            {
                var impactPanel = new StackPanel 
                { 
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                
                impactPanel.Children.Add(new TextBlock
                {
                    Text = "Expected Impact: ",
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = (Brush)FindResource("SecondaryText")
                });
                
                impactPanel.Children.Add(new TextBlock
                {
                    Text = recommendation.ExpectedImpact,
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = (Brush)FindResource("SecondaryText")
                });
                
                contentPanel.Children.Add(impactPanel);
            }

            // Action Steps (expandable)
            if (ShowActionSteps && recommendation.ActionSteps?.Count > 0)
            {
                var expander = new Expander
                {
                    Header = $"Action Steps ({recommendation.ActionSteps.Count})",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("PrimaryText"),
                    Margin = new Thickness(0, 8, 0, 0),
                    IsExpanded = recommendation.Urgency == RecommendationEngine.Urgency.Critical
                };

                var stepsPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
                
                int stepNum = 1;
                foreach (var step in recommendation.ActionSteps)
                {
                    var stepText = new TextBlock
                    {
                        Text = $"{stepNum}. {step}",
                        FontSize = 12,
                        Foreground = (Brush)FindResource("SecondaryText"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    stepsPanel.Children.Add(stepText);
                    stepNum++;
                }

                expander.Content = stepsPanel;
                contentPanel.Children.Add(expander);
            }

            card.Child = contentPanel;
            return card;
        }

        private Brush GetUrgencyBrush(RecommendationEngine.Urgency urgency)
        {
            return urgency switch
            {
                RecommendationEngine.Urgency.Critical => (Brush)FindResource("CriticalBrush"),
                RecommendationEngine.Urgency.High => (Brush)FindResource("HighBrush"),
                RecommendationEngine.Urgency.Medium => (Brush)FindResource("MediumBrush"),
                RecommendationEngine.Urgency.Low => (Brush)FindResource("LowBrush"),
                RecommendationEngine.Urgency.Informational => (Brush)FindResource("InfoBrush"),
                _ => (Brush)FindResource("InfoBrush")
            };
        }

        #endregion
    }
}
