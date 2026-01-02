using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Services.Analytics;

namespace Tracker.Controls.Analytics
{
    /// <summary>
    /// Interactive control for exploring what-if scenarios for OKR/KPI trajectories.
    /// </summary>
    public partial class WhatIfSimulatorControl : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty PredictionProperty =
            DependencyProperty.Register(
                nameof(Prediction),
                typeof(PredictiveAnalyticsService.PredictionResult),
                typeof(WhatIfSimulatorControl),
                new PropertyMetadata(null, OnPredictionChanged));

        /// <summary>
        /// The prediction result to simulate scenarios for.
        /// </summary>
        public PredictiveAnalyticsService.PredictionResult Prediction
        {
            get => (PredictiveAnalyticsService.PredictionResult)GetValue(PredictionProperty);
            set => SetValue(PredictionProperty, value);
        }

        public static readonly DependencyProperty SelectedResultProperty =
            DependencyProperty.Register(
                nameof(SelectedResult),
                typeof(WhatIfSimulator.WhatIfResult),
                typeof(WhatIfSimulatorControl),
                new PropertyMetadata(null));

        /// <summary>
        /// The currently selected simulation result.
        /// </summary>
        public WhatIfSimulator.WhatIfResult SelectedResult
        {
            get => (WhatIfSimulator.WhatIfResult)GetValue(SelectedResultProperty);
            set => SetValue(SelectedResultProperty, value);
        }

        #endregion

        private readonly WhatIfSimulator _simulator = WhatIfSimulator.Instance;

        public WhatIfSimulatorControl()
        {
            InitializeComponent();
        }

        #region Property Changed Handlers

        private static void OnPredictionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WhatIfSimulatorControl)d;
            control.UpdateDisplay();
        }

        #endregion

        #region Update Methods

        private void UpdateDisplay()
        {
            if (Prediction == null || !Prediction.IsValid)
            {
                ClearDisplay();
                return;
            }

            UpdateBaseline();
            LoadScenarios();
            UpdateCustomScenario();
        }

        private void ClearDisplay()
        {
            CurrentProgressText.Text = "--%";
            ProjectedFinalText.Text = "--%";
            DaysRemainingText.Text = "--";
            ScenariosPanel.Children.Clear();
            CustomScenarioResult.Text = "";
        }

        private void UpdateBaseline()
        {
            // Get current progress from trajectory data
            double currentProgress = Prediction.Trajectory?.CurrentProgress ?? 0;

            // Calculate days remaining from trajectory target date
            int daysRemaining = 0;
            if (Prediction.Trajectory?.TargetDate.HasValue == true)
            {
                daysRemaining = Math.Max(0, (Prediction.Trajectory.TargetDate.Value - DateTime.Today).Days);
            }

            // Calculate projected final using trajectory points
            double projectedFinal = currentProgress;
            if (Prediction.TrajectoryPoints?.Count >= 2)
            {
                var first = Prediction.TrajectoryPoints[0];
                var last = Prediction.TrajectoryPoints[^1];
                var days = (last.Date - first.Date).TotalDays;
                if (days > 0)
                {
                    var velocity = (last.ProjectedProgress - first.ProjectedProgress) / days;
                    projectedFinal = currentProgress + (velocity * daysRemaining);
                }
            }

            CurrentProgressText.Text = $"{currentProgress:F0}%";
            ProjectedFinalText.Text = $"{Math.Min(projectedFinal, 100):F0}%";
            DaysRemainingText.Text = daysRemaining.ToString();

            // Color code projected final
            if (projectedFinal >= 100)
            {
                ProjectedFinalText.Foreground = new SolidColorBrush(Colors.Green);
            }
            else if (projectedFinal >= 70)
            {
                ProjectedFinalText.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                ProjectedFinalText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void LoadScenarios()
        {
            ScenariosPanel.Children.Clear();

            var scenarios = _simulator.GetPredefinedScenarios();
            foreach (var scenario in scenarios)
            {
                var result = _simulator.Simulate(Prediction, scenario);
                var card = CreateScenarioCard(scenario, result);
                ScenariosPanel.Children.Add(card);
            }

            // Add "Required Velocity" scenario
            var requiredScenario = _simulator.CalculateRequiredVelocity(Prediction);
            if (requiredScenario.VelocityMultiplier < 10) // Skip if impossibly high
            {
                var requiredResult = _simulator.Simulate(Prediction, requiredScenario);
                var requiredCard = CreateScenarioCard(requiredScenario, requiredResult, isRequired: true);
                ScenariosPanel.Children.Insert(0, requiredCard);
            }
        }

        private Border CreateScenarioCard(
            WhatIfSimulator.WhatIfScenario scenario,
            WhatIfSimulator.WhatIfResult result,
            bool isRequired = false)
        {
            var card = new Border
            {
                Background = isRequired 
                    ? (Brush)FindResource("AccentBackgroundLight") 
                    : (Brush)FindResource("CardBackground"),
                BorderBrush = (Brush)FindResource("CardBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                Tag = result
            };

            card.MouseLeftButtonUp += ScenarioCard_Click;
            card.MouseEnter += (s, e) => card.BorderBrush = (Brush)FindResource("AccentColor");
            card.MouseLeave += (s, e) => card.BorderBrush = (Brush)FindResource("CardBorder");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left side - Scenario info
            var infoPanel = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = isRequired ? $"🎯 {scenario.Name}" : scenario.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = (Brush)FindResource("PrimaryText")
            };
            infoPanel.Children.Add(titleText);

            var descText = new TextBlock
            {
                Text = scenario.Description,
                FontSize = 11,
                Foreground = (Brush)FindResource("SecondaryText"),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(descText);

            // Impact description
            if (!string.IsNullOrEmpty(result.Impact?.ImpactDescription))
            {
                var impactText = new TextBlock
                {
                    Text = result.Impact.ImpactDescription,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("SecondaryText"),
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                infoPanel.Children.Add(impactText);
            }

            Grid.SetColumn(infoPanel, 0);
            grid.Children.Add(infoPanel);

            // Right side - Result indicator
            var resultPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var projectedText = new TextBlock
            {
                Text = $"{result.Outcome.ProjectedFinalProgress:F0}%",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = result.WillHitTarget
                    ? new SolidColorBrush(Colors.Green)
                    : new SolidColorBrush(Colors.Orange)
            };
            resultPanel.Children.Add(projectedText);

            if (result.Outcome.DaysToTarget.HasValue)
            {
                var daysText = new TextBlock
                {
                    Text = $"{result.Outcome.DaysToTarget} days",
                    FontSize = 10,
                    Foreground = (Brush)FindResource("SecondaryText"),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                resultPanel.Children.Add(daysText);
            }

            Grid.SetColumn(resultPanel, 1);
            grid.Children.Add(resultPanel);

            card.Child = grid;
            return card;
        }

        private void ScenarioCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is WhatIfSimulator.WhatIfResult result)
            {
                SelectedResult = result;
                
                // Visual feedback - highlight selected card
                foreach (var child in ScenariosPanel.Children)
                {
                    if (child is Border b)
                    {
                        b.BorderThickness = new Thickness(1);
                    }
                }
                card.BorderThickness = new Thickness(2);
            }
        }

        private void VelocitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            
            SliderValueText.Text = $"{e.NewValue:P0}";
            UpdateCustomScenario();
        }

        private void UpdateCustomScenario()
        {
            if (Prediction == null || !Prediction.IsValid)
            {
                CustomScenarioResult.Text = "";
                return;
            }

            var customScenario = _simulator.CreateCustomScenario(VelocitySlider.Value);
            var result = _simulator.Simulate(Prediction, customScenario);

            if (result.WillHitTarget)
            {
                CustomScenarioResult.Text = $"✅ At {VelocitySlider.Value:P0} velocity: " +
                    $"Projected {result.Outcome.ProjectedFinalProgress:F0}% completion. " +
                    $"Would hit target in {result.Outcome.DaysToTarget} days.";
                CustomScenarioResult.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                CustomScenarioResult.Text = $"⚠️ At {VelocitySlider.Value:P0} velocity: " +
                    $"Projected {result.Outcome.ProjectedFinalProgress:F0}% completion. " +
                    $"Would not hit 100% target.";
                CustomScenarioResult.Foreground = new SolidColorBrush(Colors.Orange);
            }

            SelectedResult = result;
        }

        #endregion
    }
}
