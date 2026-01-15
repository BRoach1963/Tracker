using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Services.AI.Insights;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the InsightPanelControl.
    /// Manages insight display, dismissal, and navigation.
    /// </summary>
    public class InsightPanelViewModel : BaseViewModel
    {
        private readonly ILogger _logger = LoggingManager.GetComponentLogger("InsightPanel");
        private ObservableCollection<Insight> _insights = new();
        private bool _isLoading;
        
        public InsightPanelViewModel()
        {
            // Don't load data in constructor - wait for Loaded event
            // Data will be loaded asynchronously to avoid blocking UI

            // Subscribe to insight updates
            try
            {
                InsightEngine.Instance.InsightsUpdated += OnInsightsUpdated;
            }
            catch (Exception ex)
            {
                _logger.Debug("InsightEngine not available: {0}", ex.Message);
            }
        }
        
        #region Properties
        
        public ObservableCollection<Insight> Insights
        {
            get => _insights;
            set { _insights = value; RaisePropertyChanged(); }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; RaisePropertyChanged(); }
        }
        
        public bool HasNoInsights => Insights.Count == 0;
        
        public bool HasUnreadInsights => Insights.Any(i => !i.IsRead);
        
        public string InsightCountText
        {
            get
            {
                var count = Insights.Count;
                var unread = Insights.Count(i => !i.IsRead);
                if (count == 0) return "No insights";
                if (unread == 0) return $"{count} insights";
                return $"{unread} unread of {count}";
            }
        }
        
        #endregion
        
        #region Commands
        
        private ICommand? _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= 
            new TrackerCommand(async _ => await RefreshAsync());
        
        private ICommand? _markAllReadCommand;
        public ICommand MarkAllReadCommand => _markAllReadCommand ??= 
            new TrackerCommand(async _ => await MarkAllReadAsync());
        
        private ICommand? _dismissInsightCommand;
        public ICommand DismissInsightCommand => _dismissInsightCommand ??= 
            new TrackerCommand(async param => await DismissInsightAsync(param as Insight));
        
        private ICommand? _openInsightCommand;
        public ICommand OpenInsightCommand => _openInsightCommand ??= 
            new TrackerCommand(async param => await OpenInsightAsync(param as Insight));
        
        #endregion
        
        #region Private Methods

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                var engine = InsightEngine.Instance;
                var insights = await engine.GetActiveInsightsAsync().ConfigureAwait(false);

                // Sort insights before updating collection
                var sortedInsights = insights
                    .OrderByDescending(i => i.Severity)
                    .ThenByDescending(i => i.GeneratedAt)
                    .ToList();

                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Replace entire collection at once instead of Clear() + Add() loop
                    Insights = new ObservableCollection<Insight>(sortedInsights);

                    RaisePropertyChanged(nameof(HasNoInsights));
                    RaisePropertyChanged(nameof(HasUnreadInsights));
                    RaisePropertyChanged(nameof(InsightCountText));
                });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to refresh insights");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task MarkAllReadAsync()
        {
            try
            {
                var engine = InsightEngine.Instance;
                foreach (var insight in Insights.Where(i => !i.IsRead).ToList())
                {
                    await engine.MarkAsReadAsync(insight.Id);
                    insight.IsRead = true;
                }
                
                RaisePropertyChanged(nameof(HasUnreadInsights));
                RaisePropertyChanged(nameof(InsightCountText));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to mark all insights as read");
            }
        }
        
        private async Task DismissInsightAsync(Insight? insight)
        {
            if (insight == null) return;
            
            try
            {
                var engine = InsightEngine.Instance;
                await engine.DismissInsightAsync(insight.Id);
                
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Insights.Remove(insight);
                    RaisePropertyChanged(nameof(HasNoInsights));
                    RaisePropertyChanged(nameof(HasUnreadInsights));
                    RaisePropertyChanged(nameof(InsightCountText));
                });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to dismiss insight {0}", insight.Id);
            }
        }
        
        private async Task OpenInsightAsync(Insight? insight)
        {
            if (insight == null) return;
            
            try
            {
                // Mark as read
                if (!insight.IsRead)
                {
                    var engine = InsightEngine.Instance;
                    await engine.MarkAsReadAsync(insight.Id);
                    insight.IsRead = true;
                    
                    RaisePropertyChanged(nameof(HasUnreadInsights));
                    RaisePropertyChanged(nameof(InsightCountText));
                }
                
                // Navigate to related entity if available
                if (!string.IsNullOrEmpty(insight.EntityType) && insight.EntityId.HasValue)
                {
                    NavigateToEntity(insight.EntityType, insight.EntityId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to open insight {0}", insight.Id);
            }
        }
        
        private void NavigateToEntity(string entityType, Guid entityId)
        {
            // TODO: Implement navigation based on entity type
            // For now, log the navigation request
            _logger.Info("Navigation requested to {0} with ID {1}", entityType, entityId);
            
            // Future: Add proper navigation enum values and handle deep-linking
            // switch (entityType.ToLowerInvariant())
            // {
            //     case "teammember": Navigate to team member view...
            //     case "oneonone": Navigate to meeting view...
            // }
        }
        
        private void OnInsightsUpdated(object? sender, int newCount)
        {
            _ = RefreshAsync();
        }
        
        #endregion
    }
}
