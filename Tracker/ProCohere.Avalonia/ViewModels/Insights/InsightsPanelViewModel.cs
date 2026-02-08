using System;
using System.Collections.Generic;
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
/// ViewModel for the AI Insights panel in Pulse.
/// Displays all insights grouped by category.
/// </summary>
public partial class InsightsPanelViewModel : ViewModelBase
{
    private readonly IInsightRepository _repository;
    private readonly IInsightActionRepository _actionRepository;

    #region Observable Properties

    /// <summary>
    /// All insights (flat list for internal use).
    /// </summary>
    private readonly List<Insight> _allInsights = new();

    /// <summary>
    /// Grouped insights for display.
    /// </summary>
    public ObservableCollection<InsightGroup> GroupedInsights { get; } = new();

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
    /// Whether there are any insights.
    /// </summary>
    public bool HasInsights => _allInsights.Count > 0;

    /// <summary>
    /// Whether to show the empty state.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasInsights;

    /// <summary>
    /// Total insight count for badge display.
    /// </summary>
    public int TotalCount => _allInsights.Count;

    /// <summary>
    /// Critical count for badge emphasis.
    /// </summary>
    public int CriticalCount => _allInsights.Count(i => i.IsCritical);

    #endregion

    #region Events

    /// <summary>
    /// Raised when user wants to navigate to an entity.
    /// </summary>
    public event EventHandler<(string EntityType, Guid EntityId)>? NavigateRequested;

    #endregion

    #region Constructor

    public InsightsPanelViewModel() : this(new InsightRepository(), CreateActionRepository())
    {
    }

    public InsightsPanelViewModel(IInsightRepository repository, IInsightActionRepository actionRepository)
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
    /// Loads all insights for the current user.
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

            var insights = await _repository.GetActiveInsightsAsync(teamMember.Id);

            _allInsights.Clear();
            _allInsights.AddRange(insights.OrderByDescending(i => i.SeverityLevel).ThenByDescending(i => i.CreatedAt));

            RebuildGroups();
            NotifyPropertyChanges();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load insights";
            System.Diagnostics.Debug.WriteLine($"[InsightsPanelViewModel] Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the insight list.
    /// </summary>
    [RelayCommand]
    public Task RefreshAsync() => LoadAsync();

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
            RemoveInsight(insight);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InsightsPanelViewModel] Dismiss failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SnoozeInsight(Insight? insight)
    {
        if (insight == null || string.IsNullOrEmpty(insight.SignatureHash))
            return;

        try
        {
            await _actionRepository.SnoozeAsync(insight.SignatureHash, TimeSpan.FromHours(24), insightId: insight.Id);
            RemoveInsight(insight);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InsightsPanelViewModel] Snooze failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[InsightsPanelViewModel] MarkActed failed: {ex.Message}");
            }
        }

        // Navigate if has source
        if (insight.SourceId.HasValue && !string.IsNullOrEmpty(insight.SourceType))
        {
            NavigateRequested?.Invoke(this, (insight.SourceType, insight.SourceId.Value));
        }

        RemoveInsight(insight);
    }

    #endregion

    #region Private Methods

    private void RemoveInsight(Insight insight)
    {
        _allInsights.Remove(insight);
        RebuildGroups();
        NotifyPropertyChanges();
    }

    private void RebuildGroups()
    {
        GroupedInsights.Clear();

        var groupOrder = new[]
        {
            ("Tasks", new[] { InsightType.TaskOverdue, InsightType.StaleActionItem }),
            ("Goals", new[] { InsightType.GoalOffTrack, InsightType.GoalOnTrack }),
            ("Meetings", new[] { InsightType.MeetingOverdue, InsightType.MeetingUpcoming }),
            ("Metrics", new[] { InsightType.MetricMissing, InsightType.MetricDeclining }),
            ("Team", new[] { InsightType.PersonalDate, InsightType.SentimentDeclining, InsightType.SentimentImproving })
        };

        foreach (var (groupName, types) in groupOrder)
        {
            var groupInsights = _allInsights
                .Where(i => types.Contains(i.Type))
                .ToList();

            if (groupInsights.Count > 0)
            {
                GroupedInsights.Add(new InsightGroup(groupName, groupInsights));
            }
        }
    }

    private void NotifyPropertyChanges()
    {
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(CriticalCount));
    }

    #endregion
}

/// <summary>
/// Represents a group of insights by category.
/// </summary>
public class InsightGroup
{
    public string Name { get; }
    public IReadOnlyList<Insight> Insights { get; }
    public int Count => Insights.Count;
    public bool HasCritical => Insights.Any(i => i.IsCritical);

    public InsightGroup(string name, IReadOnlyList<Insight> insights)
    {
        Name = name;
        Insights = insights;
    }
}
