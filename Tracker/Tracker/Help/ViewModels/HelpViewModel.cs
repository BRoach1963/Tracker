using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Help.Models;
using Tracker.Help.Services;
using Tracker.Logging;
using Tracker.ViewModels;

namespace Tracker.Help.ViewModels
{
    /// <summary>
    /// ViewModel for the Help Window.
    /// </summary>
    public class HelpViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly MarkdownRenderer _renderer;
        private readonly Stack<string> _backStack = new();
        private readonly Stack<string> _forwardStack = new();

        private string _currentTopicId = string.Empty;
        private HelpTopic? _currentTopic;
        private FlowDocument? _document;
        private string _searchQuery = string.Empty;
        private bool _isSearching;
        private ObservableCollection<HelpSearchResult> _searchResults = new();
        private ObservableCollection<HelpTocEntry> _tocEntries = new();
        private HelpTocEntry? _selectedTocEntry;
        private string _breadcrumb = string.Empty;

        private ICommand? _backCommand;
        private ICommand? _forwardCommand;
        private ICommand? _homeCommand;
        private ICommand? _searchCommand;
        private ICommand? _clearSearchCommand;
        private ICommand? _navigateToResultCommand;
        private ICommand? _zoomInCommand;
        private ICommand? _zoomOutCommand;
        private ICommand? _zoomResetCommand;

        private double _zoomLevel = 100.0;
        private const double MinZoom = 50.0;
        private const double MaxZoom = 200.0;
        private const double ZoomStep = 10.0;

        #endregion

        #region Constructor

        public HelpViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("HelpViewModel");
            _renderer = new MarkdownRenderer();
            _renderer.TopicLinkClicked += OnTopicLinkClicked;

            LoadTableOfContents();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderer.TopicLinkClicked -= OnTopicLinkClicked;
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Properties

        public string CurrentTopicId
        {
            get => _currentTopicId;
            private set
            {
                _currentTopicId = value;
                RaisePropertyChanged();
            }
        }

        public HelpTopic? CurrentTopic
        {
            get => _currentTopic;
            private set
            {
                _currentTopic = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Title));
            }
        }

        public string Title => _currentTopic?.Title ?? "Help";

        public FlowDocument? Document
        {
            get => _document;
            private set
            {
                _document = value;
                RaisePropertyChanged();
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSearchQuery));
            }
        }

        public bool HasSearchQuery => !string.IsNullOrWhiteSpace(_searchQuery);

        public bool IsSearching
        {
            get => _isSearching;
            private set
            {
                _isSearching = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<HelpSearchResult> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSearchResults));
                RaisePropertyChanged(nameof(ShowSearchResults));
            }
        }

        public bool HasSearchResults => _searchResults.Count > 0;
        public bool ShowSearchResults => HasSearchQuery && HasSearchResults;

        public ObservableCollection<HelpTocEntry> TocEntries
        {
            get => _tocEntries;
            set
            {
                _tocEntries = value;
                RaisePropertyChanged();
            }
        }

        public HelpTocEntry? SelectedTocEntry
        {
            get => _selectedTocEntry;
            set
            {
                _selectedTocEntry = value;
                RaisePropertyChanged();

                // Navigate to selected topic
                if (value?.TopicId != null && value.TopicId != _currentTopicId)
                {
                    _ = NavigateToTopicAsync(value.TopicId);
                }
            }
        }

        public string Breadcrumb
        {
            get => _breadcrumb;
            private set
            {
                _breadcrumb = value;
                RaisePropertyChanged();
            }
        }

        public bool CanGoBack => _backStack.Count > 0;
        public bool CanGoForward => _forwardStack.Count > 0;

        /// <summary>
        /// Current zoom level as a percentage (50-200).
        /// </summary>
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                var clamped = Math.Max(MinZoom, Math.Min(MaxZoom, value));
                if (Math.Abs(_zoomLevel - clamped) > 0.1)
                {
                    _zoomLevel = clamped;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ZoomScale));
                    RaisePropertyChanged(nameof(ZoomDisplayText));
                    RaisePropertyChanged(nameof(CanZoomIn));
                    RaisePropertyChanged(nameof(CanZoomOut));
                }
            }
        }

        /// <summary>
        /// Zoom scale factor for transforms (1.0 = 100%).
        /// </summary>
        public double ZoomScale => _zoomLevel / 100.0;

        /// <summary>
        /// Display text for zoom level (e.g., "100%").
        /// </summary>
        public string ZoomDisplayText => $"{_zoomLevel:0}%";

        public bool CanZoomIn => _zoomLevel < MaxZoom;
        public bool CanZoomOut => _zoomLevel > MinZoom;

        #endregion

        #region Commands

        public ICommand BackCommand => _backCommand ??= new TrackerCommand(
            _ => GoBack(),
            _ => CanGoBack);

        public ICommand ForwardCommand => _forwardCommand ??= new TrackerCommand(
            _ => GoForward(),
            _ => CanGoForward);

        public ICommand HomeCommand => _homeCommand ??= new TrackerCommand(
            _ => { _ = NavigateToTopicAsync(HelpTopicRegistry.Instance.DefaultTopicId); });

        public ICommand SearchCommand => _searchCommand ??= new AsyncCommand(
            async _ => await PerformSearchAsync(),
            _ => !string.IsNullOrWhiteSpace(_searchQuery));

        public ICommand ClearSearchCommand => _clearSearchCommand ??= new TrackerCommand(
            _ => ClearSearch());

        public ICommand NavigateToResultCommand => _navigateToResultCommand ??= new TrackerCommand(
            param =>
            {
                if (param is HelpSearchResult result)
                {
                    ClearSearch();
                    _ = NavigateToTopicAsync(result.TopicId, result.SectionId);
                }
            });

        public ICommand ZoomInCommand => _zoomInCommand ??= new TrackerCommand(
            _ => ZoomLevel += ZoomStep,
            _ => CanZoomIn);

        public ICommand ZoomOutCommand => _zoomOutCommand ??= new TrackerCommand(
            _ => ZoomLevel -= ZoomStep,
            _ => CanZoomOut);

        public ICommand ZoomResetCommand => _zoomResetCommand ??= new TrackerCommand(
            _ => ZoomLevel = 100.0);

        #endregion

        #region Public Methods

        /// <summary>
        /// Navigates to a specific help topic.
        /// </summary>
        public async Task NavigateToTopicAsync(string topicId, string? section = null)
        {
            if (string.IsNullOrEmpty(topicId)) return;

            try
            {
                // Save current topic to back stack
                if (!string.IsNullOrEmpty(_currentTopicId) && _currentTopicId != topicId)
                {
                    _backStack.Push(_currentTopicId);
                    _forwardStack.Clear();
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoForward));
                }

                var topic = await HelpService.Instance.GetTopicAsync(topicId);
                if (topic == null)
                {
                    _logger.Warn("Topic not found: {0}", topicId);
                    Document = _renderer.Render($"# Topic Not Found\n\nThe help topic '{topicId}' could not be found.");
                    return;
                }

                CurrentTopicId = topicId;
                CurrentTopic = topic;
                Document = _renderer.Render(topic.Content);
                UpdateBreadcrumb(topic);
                SelectTocEntry(topicId);

                _logger.Info("Navigated to help topic: {0}", topicId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error navigating to topic: {0}", topicId);
                Document = _renderer.Render($"# Error\n\nFailed to load help topic: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the help for a specific context.
        /// </summary>
        public async Task ShowContextHelpAsync(HelpContext context)
        {
            await NavigateToTopicAsync(context.TopicId, context.Section);
        }

        #endregion

        #region Private Methods

        private void LoadTableOfContents()
        {
            var toc = HelpService.Instance.GetTableOfContents();
            if (toc?.Children != null)
            {
                TocEntries = new ObservableCollection<HelpTocEntry>(toc.Children);
            }
        }

        private void GoBack()
        {
            if (_backStack.Count == 0) return;

            var previousTopic = _backStack.Pop();
            if (!string.IsNullOrEmpty(_currentTopicId))
            {
                _forwardStack.Push(_currentTopicId);
            }

            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoForward));

            // Navigate without adding to back stack
            _ = NavigateWithoutHistoryAsync(previousTopic);
        }

        private void GoForward()
        {
            if (_forwardStack.Count == 0) return;

            var nextTopic = _forwardStack.Pop();
            if (!string.IsNullOrEmpty(_currentTopicId))
            {
                _backStack.Push(_currentTopicId);
            }

            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoForward));

            _ = NavigateWithoutHistoryAsync(nextTopic);
        }

        private async Task NavigateWithoutHistoryAsync(string topicId)
        {
            var topic = await HelpService.Instance.GetTopicAsync(topicId);
            if (topic == null) return;

            CurrentTopicId = topicId;
            CurrentTopic = topic;
            Document = _renderer.Render(topic.Content);
            UpdateBreadcrumb(topic);
            SelectTocEntry(topicId);
        }

        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery)) return;

            try
            {
                IsSearching = true;
                var results = await HelpService.Instance.SearchAsync(_searchQuery);
                SearchResults = new ObservableCollection<HelpSearchResult>(results);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Search failed");
                SearchResults = new ObservableCollection<HelpSearchResult>();
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            SearchResults = new ObservableCollection<HelpSearchResult>();
        }

        private void UpdateBreadcrumb(HelpTopic topic)
        {
            var parts = topic.Id.Split('/');
            var breadcrumbParts = new List<string> { "Help" };

            foreach (var part in parts)
            {
                breadcrumbParts.Add(ToTitleCase(part));
            }

            Breadcrumb = string.Join(" > ", breadcrumbParts);
        }

        private void SelectTocEntry(string topicId)
        {
            // Find and select the TOC entry matching the topic
            foreach (var category in TocEntries)
            {
                if (category.TopicId == topicId)
                {
                    _selectedTocEntry = category;
                    RaisePropertyChanged(nameof(SelectedTocEntry));
                    return;
                }

                foreach (var child in category.Children)
                {
                    if (child.TopicId == topicId)
                    {
                        category.IsExpanded = true;
                        _selectedTocEntry = child;
                        RaisePropertyChanged(nameof(SelectedTocEntry));
                        return;
                    }
                }
            }
        }

        private void OnTopicLinkClicked(object? sender, string topicId)
        {
            // Resolve relative paths
            if (topicId.StartsWith("../"))
            {
                // Navigate up from current topic
                var currentParts = _currentTopicId.Split('/');
                var newParts = topicId.Split('/').Where(p => p != "..").ToList();
                
                if (currentParts.Length > 1)
                {
                    topicId = string.Join("/", currentParts.Take(currentParts.Length - 1).Concat(newParts));
                }
            }

            _ = NavigateToTopicAsync(topicId);
        }

        private static string ToTitleCase(string text)
        {
            return string.Join(" ", text.Split('-', '_')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        #endregion
    }
}

