using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Logging;
using Tracker.Services;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the global search feature.
    /// </summary>
    public class SearchViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("SearchVM");
        private string _searchQuery = string.Empty;
        private ObservableCollection<SearchResult> _results = new();
        private ObservableCollection<SearchResult> _recentItems = new();
        private SearchResult? _selectedResult;
        private bool _isSearching;
        private bool _hasSearched;

        private ICommand? _searchCommand;
        private ICommand? _clearCommand;
        private ICommand? _openResultCommand;

        private System.Timers.Timer? _debounceTimer;

        #endregion

        #region Constructor

        public SearchViewModel()
        {
            LoadRecentItems();
            
            // Setup debounce timer for live search
            _debounceTimer = new System.Timers.Timer(300);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += async (s, e) =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await PerformSearch();
                });
            };
        }

        #endregion

        #region Properties

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                RaisePropertyChanged();
                
                // Trigger debounced search
                _debounceTimer?.Stop();
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    _debounceTimer?.Start();
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    _results.Clear();
                    _hasSearched = false;
                    RaisePropertyChanged(nameof(HasResults));
                    RaisePropertyChanged(nameof(ShowNoResults));
                    RaisePropertyChanged(nameof(ShowRecentItems));
                }
            }
        }

        public ObservableCollection<SearchResult> Results => _results;

        public ObservableCollection<SearchResult> RecentItems => _recentItems;

        public SearchResult? SelectedResult
        {
            get => _selectedResult;
            set
            {
                _selectedResult = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                RaisePropertyChanged();
            }
        }

        public bool HasResults => _results.Count > 0;

        public bool ShowNoResults => _hasSearched && _results.Count == 0 && !string.IsNullOrWhiteSpace(_searchQuery);

        public bool ShowRecentItems => !_hasSearched && string.IsNullOrWhiteSpace(_searchQuery) && _recentItems.Count > 0;

        public int ResultCount => _results.Count;

        #endregion

        #region Commands

        public ICommand SearchCommand =>
            _searchCommand ??= new TrackerCommand(async _ => await PerformSearch(), _ => !string.IsNullOrWhiteSpace(SearchQuery));

        public ICommand ClearCommand =>
            _clearCommand ??= new TrackerCommand(_ => ClearSearch());

        public ICommand OpenResultCommand =>
            _openResultCommand ??= new TrackerCommand(OpenResultExecuted, _ => SelectedResult != null);

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs the search.
        /// </summary>
        public async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery) || _searchQuery.Length < 2)
                return;

            IsSearching = true;

            try
            {
                var results = await SearchService.Instance.SearchAsync(_searchQuery);
                
                _results.Clear();
                foreach (var result in results)
                {
                    _results.Add(result);
                }

                _hasSearched = true;
            }
            finally
            {
                IsSearching = false;
                RaisePropertyChanged(nameof(HasResults));
                RaisePropertyChanged(nameof(ShowNoResults));
                RaisePropertyChanged(nameof(ShowRecentItems));
                RaisePropertyChanged(nameof(ResultCount));
            }
        }

        /// <summary>
        /// Clears the search.
        /// </summary>
        public void ClearSearch()
        {
            SearchQuery = string.Empty;
            _results.Clear();
            _hasSearched = false;
            RaisePropertyChanged(nameof(HasResults));
            RaisePropertyChanged(nameof(ShowNoResults));
            RaisePropertyChanged(nameof(ShowRecentItems));
        }

        #endregion

        #region Private Methods

        private async void LoadRecentItems()
        {
            try
            {
                var recent = await SearchService.Instance.GetRecentItemsAsync(10);
                _recentItems.Clear();
                foreach (var item in recent)
                {
                    _recentItems.Add(item);
                }
                RaisePropertyChanged(nameof(ShowRecentItems));
            }
            catch
            {
                // Ignore errors loading recent items
            }
        }

        private void OpenResultExecuted(object? parameter)
        {
            if (parameter is SearchResult result)
            {
                SelectedResult = result;
            }
            
            // Navigation logic will be handled by the view
            // The view can subscribe to SelectedResult changes
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _debounceTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

