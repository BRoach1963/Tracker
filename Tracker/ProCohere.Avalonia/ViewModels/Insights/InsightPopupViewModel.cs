using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Services.Insights;

namespace ProCohere.Avalonia.ViewModels.Insights;

/// <summary>
/// ViewModel for the startup insights popup dialog.
/// Shows top N critical insights requiring immediate attention.
/// </summary>
public partial class InsightPopupViewModel : ViewModelBase
{
    private const int MaxInsights = 5;
    private const int MinSeverity = 4;
    
    private readonly IInsightRepository _repository;
    private readonly IInsightActionRepository _actionRepository;

    #region Observable Properties

    /// <summary>
    /// Critical insights to display.
    /// </summary>
    public ObservableCollection<Insight> Insights { get; } = new();

    /// <summary>
    /// Whether data is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Whether there are any insights to show.
    /// </summary>
    public bool HasInsights => Insights.Count > 0;

    /// <summary>
    /// Whether to show the empty state.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasInsights;

    /// <summary>
    /// Count display text.
    /// </summary>
    public string CountText => Insights.Count switch
    {
        1 => "1 item needs attention",
        _ => $"{Insights.Count} items need attention"
    };

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised when user wants to navigate to an entity.
    /// </summary>
    public event EventHandler<(string EntityType, Guid EntityId)>? NavigateRequested;

    /// <summary>
    /// Raised when user wants to view all insights (navigate to Pulse).
    /// </summary>
    public event EventHandler? ViewAllRequested;

    #endregion

    #region Constructor

    public InsightPopupViewModel() : this(new InsightRepository(), CreateActionRepository())
    {
    }

    public InsightPopupViewModel(IInsightRepository repository, IInsightActionRepository actionRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
    }

    private static IInsightActionRepository CreateActionRepository()
    {
        var rpcService = new InsightRpcService();
        return new InsightActionRepository(rpcService);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads top critical insights for the current user.
    /// Call this after dialog is shown.
    /// </summary>
    public async Task LoadAsync()
    {
        var teamMember = AuthService.Instance.CurrentTeamMember;
        if (teamMember == null)
        {
            ErrorMessage = "Not logged in";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var insights = await _repository.GetTopInsightsAsync(
                teamMember.Id, 
                MaxInsights, 
                MinSeverity);

            Insights.Clear();
            foreach (var insight in insights.OrderByDescending(i => i.SeverityLevel))
            {
                Insights.Add(insight);
            }

            OnPropertyChanged(nameof(HasInsights));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(CountText));
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load insights";
            System.Diagnostics.Debug.WriteLine($"[InsightPopupViewModel] Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Checks if there are critical insights without loading the full list.
    /// Used to decide whether to show the popup.
    /// </summary>
    public static async Task<bool> HasCriticalInsightsAsync()
    {
        var teamMember = AuthService.Instance.CurrentTeamMember;
        if (teamMember == null)
            return false;

        try
        {
            var repository = new InsightRepository();
            var insights = await repository.GetTopInsightsAsync(teamMember.Id, 1, MinSeverity);
            return insights.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task DismissInsight(Insight? insight)
    {
        if (insight == null || string.IsNullOrEmpty(insight.SignatureHash))
            return;

        try
        {
            await _actionRepository.DismissAsync(insight.SignatureHash, insightId: insight.Id);
            Insights.Remove(insight);
            OnPropertyChanged(nameof(HasInsights));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(CountText));

            // Auto-close if all dismissed
            if (!HasInsights)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InsightPopupViewModel] Dismiss failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewInsight(Insight? insight)
    {
        if (insight == null)
            return;

        // Mark as acted
        if (!string.IsNullOrEmpty(insight.SignatureHash))
        {
            try
            {
                await _actionRepository.MarkActedAsync(insight.SignatureHash, insightId: insight.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InsightPopupViewModel] MarkActed failed: {ex.Message}");
            }
        }

        // Navigate if has source
        if (insight.SourceId.HasValue && !string.IsNullOrEmpty(insight.SourceType))
        {
            NavigateRequested?.Invoke(this, (insight.SourceType, insight.SourceId.Value));
        }

        // Remove from list and close
        Insights.Remove(insight);
        OnPropertyChanged(nameof(HasInsights));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ViewAll()
    {
        ViewAllRequested?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
