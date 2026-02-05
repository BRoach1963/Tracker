using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;
using System;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the What-If Scenario dialog.
/// Allows users to simulate hypothetical changes to goal trajectories.
/// </summary>
public partial class WhatIfDialogViewModel : ObservableObject
{
    private readonly WhatIfSimulator _simulator = new();
    private TrajectoryResult? _currentTrajectory;

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;

    #region Observable Properties

    /// <summary>
    /// Goal title for display.
    /// </summary>
    [ObservableProperty]
    private string _goalTitle = string.Empty;

    /// <summary>
    /// Current completion probability before simulation.
    /// </summary>
    [ObservableProperty]
    private string _currentProbabilityDisplay = "0%";

    /// <summary>
    /// Current trajectory status before simulation.
    /// </summary>
    [ObservableProperty]
    private string _currentStatusDisplay = "Unknown";

    /// <summary>
    /// Selected scenario type (0=Velocity, 1=Timeline, 2=Target).
    /// </summary>
    [ObservableProperty]
    private int _selectedScenarioIndex;

    /// <summary>
    /// Velocity adjustment percentage (-50 to +100).
    /// </summary>
    [ObservableProperty]
    private double _velocityAdjustment;

    /// <summary>
    /// Timeline adjustment in days (-30 to +60).
    /// </summary>
    [ObservableProperty]
    private int _timelineAdjustmentDays;

    /// <summary>
    /// Target adjustment percentage (-50 to +50).
    /// </summary>
    [ObservableProperty]
    private double _targetAdjustment;

    /// <summary>
    /// Whether simulation is running.
    /// </summary>
    [ObservableProperty]
    private bool _isSimulating;

    /// <summary>
    /// Whether we have a simulation result.
    /// </summary>
    [ObservableProperty]
    private bool _hasResult;

    /// <summary>
    /// The simulation result.
    /// </summary>
    [ObservableProperty]
    private ScenarioResult? _result;

    /// <summary>
    /// Simulated probability display.
    /// </summary>
    [ObservableProperty]
    private string _simulatedProbabilityDisplay = "—";

    /// <summary>
    /// Probability change display with sign.
    /// </summary>
    [ObservableProperty]
    private string _probabilityChangeDisplay = "—";

    /// <summary>
    /// Simulated status display.
    /// </summary>
    [ObservableProperty]
    private string _simulatedStatusDisplay = "—";

    /// <summary>
    /// Impact description.
    /// </summary>
    [ObservableProperty]
    private string _impactDescription = string.Empty;

    /// <summary>
    /// Whether the change is positive.
    /// </summary>
    [ObservableProperty]
    private bool _isPositiveChange;

    /// <summary>
    /// Scenario description text.
    /// </summary>
    [ObservableProperty]
    private string _scenarioDescription = string.Empty;

    #endregion

    #region Computed Display Properties

    /// <summary>
    /// Display text for velocity slider.
    /// </summary>
    public string VelocityDisplayText => VelocityAdjustment switch
    {
        > 0 => $"+{VelocityAdjustment:F0}% faster",
        < 0 => $"{VelocityAdjustment:F0}% slower",
        _ => "No change"
    };

    /// <summary>
    /// Display text for timeline slider.
    /// </summary>
    public string TimelineDisplayText => TimelineAdjustmentDays switch
    {
        > 0 => TimelineAdjustmentDays == 1 ? "+1 day" : $"+{TimelineAdjustmentDays} days",
        < 0 => TimelineAdjustmentDays == -1 ? "-1 day" : $"{TimelineAdjustmentDays} days",
        _ => "No change"
    };

    /// <summary>
    /// Display text for target slider.
    /// </summary>
    public string TargetDisplayText => TargetAdjustment switch
    {
        > 0 => $"+{TargetAdjustment:F0}% higher target",
        < 0 => $"{TargetAdjustment:F0}% lower target",
        _ => "No change"
    };

    #endregion

    /// <summary>
    /// Initializes the dialog with a trajectory to simulate.
    /// </summary>
    public void Initialize(TrajectoryResult trajectory)
    {
        _currentTrajectory = trajectory;
        GoalTitle = trajectory.GoalTitle;
        CurrentProbabilityDisplay = trajectory.ProbabilityDisplay;
        CurrentStatusDisplay = trajectory.StatusDisplay;

        // Reset inputs
        VelocityAdjustment = 0;
        TimelineAdjustmentDays = 0;
        TargetAdjustment = 0;
        SelectedScenarioIndex = 0;

        // Clear previous results
        ClearResult();
    }

    /// <summary>
    /// Called when velocity adjustment changes.
    /// </summary>
    partial void OnVelocityAdjustmentChanged(double value)
    {
        OnPropertyChanged(nameof(VelocityDisplayText));
        if (SelectedScenarioIndex == 0)
        {
            RunSimulationInternal();
        }
    }

    /// <summary>
    /// Called when timeline adjustment changes.
    /// </summary>
    partial void OnTimelineAdjustmentDaysChanged(int value)
    {
        OnPropertyChanged(nameof(TimelineDisplayText));
        if (SelectedScenarioIndex == 1)
        {
            RunSimulationInternal();
        }
    }

    /// <summary>
    /// Called when target adjustment changes.
    /// </summary>
    partial void OnTargetAdjustmentChanged(double value)
    {
        OnPropertyChanged(nameof(TargetDisplayText));
        if (SelectedScenarioIndex == 2)
        {
            RunSimulationInternal();
        }
    }

    /// <summary>
    /// Called when scenario type changes.
    /// </summary>
    partial void OnSelectedScenarioIndexChanged(int value)
    {
        RunSimulationInternal();
    }

    /// <summary>
    /// Runs the simulation based on current inputs.
    /// </summary>
    [RelayCommand]
    private void RunSimulation()
    {
        RunSimulationInternal();
    }

    private void RunSimulationInternal()
    {
        if (_currentTrajectory == null)
        {
            ClearResult();
            return;
        }

        IsSimulating = true;

        try
        {
            ScenarioResult result = SelectedScenarioIndex switch
            {
                0 => _simulator.SimulateVelocityChange(_currentTrajectory, VelocityAdjustment),
                1 => _simulator.SimulateTimelineChange(_currentTrajectory, TimelineAdjustmentDays),
                2 => _simulator.SimulateTargetChange(_currentTrajectory, TargetAdjustment),
                _ => throw new InvalidOperationException("Unknown scenario type")
            };

            Result = result;
            SimulatedProbabilityDisplay = result.SimulatedProbabilityDisplay;
            ProbabilityChangeDisplay = result.ProbabilityChangeDisplay;
            SimulatedStatusDisplay = result.SimulatedStatusDisplay;
            ImpactDescription = result.Impact;
            IsPositiveChange = result.IsPositiveChange;
            ScenarioDescription = result.ScenarioDescription;
            HasResult = true;
        }
        catch
        {
            ClearResult();
        }
        finally
        {
            IsSimulating = false;
        }
    }

    private void ClearResult()
    {
        Result = null;
        SimulatedProbabilityDisplay = "—";
        ProbabilityChangeDisplay = "—";
        SimulatedStatusDisplay = "—";
        ImpactDescription = string.Empty;
        IsPositiveChange = false;
        ScenarioDescription = string.Empty;
        HasResult = false;
    }

    /// <summary>
    /// Resets all inputs to default values.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        VelocityAdjustment = 0;
        TimelineAdjustmentDays = 0;
        TargetAdjustment = 0;
        ClearResult();
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
