using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using ProCohere.Avalonia.Dialogs;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Attributes;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Code-behind for PulseView - the synthesis feed with quick access strip.
/// 
/// MVVM: 
/// - ViewModel is provided by MainWindowViewModel, not created here.
/// - Navigation is handled by ViewModel events (SignalNavigationRequested).
/// - View only handles data loading trigger on DataContext change.
/// </summary>
[HelpContext("pulse-view", ContextName = "PulseView")]
public partial class PulseView : UserControl
{
    private PulseViewModel? _viewModel;
    private bool _isInitialized;

    public PulseView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Log("[PulseView] Constructor");
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.CreateSurveyRequested -= OnCreateSurveyRequested;
            _viewModel.DistributeSurveyRequested -= OnDistributeSurveyRequested;
            _viewModel.CloseSurveyRequested -= OnCloseSurveyRequested;
            _viewModel.ViewAnalyticsRequested -= OnViewAnalyticsRequested;
        }
        
        _viewModel = DataContext as PulseViewModel;
        
        if (_viewModel != null)
        {
            // Subscribe to events
            _viewModel.CreateSurveyRequested += OnCreateSurveyRequested;
            _viewModel.DistributeSurveyRequested += OnDistributeSurveyRequested;
            _viewModel.CloseSurveyRequested += OnCloseSurveyRequested;
            _viewModel.ViewAnalyticsRequested += OnViewAnalyticsRequested;
            
            // Load data on first initialization
            if (!_isInitialized)
            {
                _isInitialized = true;
                Log("[PulseView] ViewModel bound, loading data");
                _ = _viewModel.LoadPulseDataCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnCreateSurveyRequested(object? sender, EventArgs e)
    {
        Log("[PulseView] Create survey requested");
        
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
        {
            Log("[PulseView] ERROR: Could not get parent window");
            return;
        }

        var survey = await AppDialogService.ShowCreateSurveyAsync(window);
        
        if (survey != null)
        {
            Log($"[PulseView] Survey created: {survey.Title} with {survey.Questions.Count} questions");
            await (_viewModel?.OnSurveyCreatedAsync() ?? Task.CompletedTask);
        }
        else
        {
            Log("[PulseView] Survey creation cancelled");
        }
    }

    private async void OnDistributeSurveyRequested(object? sender, Guid surveyId)
    {
        Log($"[PulseView] Distribute survey requested: {surveyId}");
        
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null)
        {
            Log("[PulseView] ERROR: Could not get parent window or ViewModel");
            return;
        }

        // Show confirmation dialog
        var confirmed = await AppDialogService.ShowConfirmationAsync(
            window,
            "Distribute Survey",
            "This will create response records for all target team members and activate the survey. Are you sure?",
            "Distribute",
            "Cancel");

        if (!confirmed)
        {
            Log("[PulseView] Distribution cancelled");
            return;
        }

        // Distribute survey
        var success = await SurveyService.Instance.DistributeSurveyAsync(surveyId);
        
        if (success)
        {
            Log($"[PulseView] Survey distributed successfully: {surveyId}");
            await _viewModel.OnSurveyDistributedAsync();
            
            // Show success message
            await AppDialogService.ShowConfirmationAsync(
                window,
                "Survey Distributed",
                "The survey has been distributed to target team members.",
                "OK");
        }
        else
        {
            Log($"[PulseView] ERROR distributing survey: {SurveyService.Instance.LastError}");
            await AppDialogService.ShowConfirmationAsync(
                window,
                "Distribution Failed",
                $"Failed to distribute survey: {SurveyService.Instance.LastError}",
                "OK");
        }
    }

    private async void OnCloseSurveyRequested(object? sender, Guid surveyId)
    {
        Log($"[PulseView] Close survey requested: {surveyId}");
        
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null)
        {
            Log("[PulseView] ERROR: Could not get parent window or ViewModel");
            return;
        }

        // Show confirmation dialog
        var confirmed = await AppDialogService.ShowConfirmationAsync(
            window,
            "Close Survey",
            "This will stop accepting new responses. Are you sure?",
            "Close",
            "Cancel");

        if (!confirmed)
        {
            Log("[PulseView] Close cancelled");
            return;
        }

        // Close survey
        var success = await SurveyService.Instance.CloseSurveyAsync(surveyId);
        
        if (success)
        {
            Log($"[PulseView] Survey closed successfully: {surveyId}");
            await _viewModel.OnSurveyClosedAsync();
        }
        else
        {
            Log($"[PulseView] ERROR closing survey: {SurveyService.Instance.LastError}");
            await AppDialogService.ShowConfirmationAsync(
                window,
                "Close Failed",
                $"Failed to close survey: {SurveyService.Instance.LastError}",
                "OK");
        }
    }

    private void OnViewAnalyticsRequested(object? sender, Guid surveyId)
    {
        Log($"[PulseView] View analytics requested for survey {surveyId}");

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
        {
            Log("[PulseView] ERROR: Could not get parent window");
            return;
        }

        try
        {
            var dialog = new SurveyAnalyticsDialog(surveyId);
            _ = dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Log($"[PulseView] ERROR showing analytics: {ex.Message}");
        }
    }

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "pulse_view.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion
}
