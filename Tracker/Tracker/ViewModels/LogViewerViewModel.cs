using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Helpers;
using Tracker.Logging;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the log viewer control.
    /// </summary>
    public class LogViewerViewModel : BaseViewModel
    {
        #region Fields

        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Today;
        private string _searchText = string.Empty;
        private string _selectedLevel = "All";
        private string _statusMessage = "Ready";
        private LogFileInfo? _selectedLogFile;
        private ObservableCollection<LogFileInfo> _logFiles = new();
        private ObservableCollection<LogEntryParsed> _logEntries = new();

        private ICommand? _searchCommand;
        private ICommand? _refreshCommand;
        private ICommand? _clearOldLogsCommand;
        private ICommand? _openFolderCommand;

        #endregion

        #region Constructor

        public LogViewerViewModel()
        {
            LoadLogFiles();
            Search();
        }

        #endregion

        #region Properties

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                RaisePropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                RaisePropertyChanged();
            }
        }

        public List<string> LogLevels => new() { "All", "Debug", "Info", "Warn", "Error" };

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged();
            }
        }

        public string LogDirectory => LoggingManager.Instance.LogDirectory;

        public int TotalEntries => _logEntries.Count;

        public LogFileInfo? SelectedLogFile
        {
            get => _selectedLogFile;
            set
            {
                _selectedLogFile = value;
                RaisePropertyChanged();
                if (value != null)
                {
                    StartDate = value.Date;
                    EndDate = value.Date;
                    Search();
                }
            }
        }

        public ObservableCollection<LogFileInfo> LogFiles
        {
            get => _logFiles;
            set
            {
                _logFiles = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<LogEntryParsed> LogEntries
        {
            get => _logEntries;
            set
            {
                _logEntries = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalEntries));
            }
        }

        #endregion

        #region Commands

        public ICommand SearchCommand => _searchCommand ??= new TrackerCommand(_ => Search());
        public ICommand RefreshCommand => _refreshCommand ??= new TrackerCommand(_ => Refresh());
        public ICommand ClearOldLogsCommand => _clearOldLogsCommand ??= new TrackerCommand(_ => ClearOldLogs());
        public ICommand OpenFolderCommand => _openFolderCommand ??= new TrackerCommand(_ => OpenFolder());

        #endregion

        #region Methods

        private void LoadLogFiles()
        {
            _logFiles.Clear();
            var files = LoggingManager.Instance.GetLogFiles();
            foreach (var file in files)
            {
                _logFiles.Add(file);
            }
            RaisePropertyChanged(nameof(LogFiles));
        }

        private void Search()
        {
            try
            {
                StatusMessage = "Searching...";

                LogLevel? minLevel = _selectedLevel switch
                {
                    "Debug" => LogLevel.Debug,
                    "Info" => LogLevel.Info,
                    "Warn" => LogLevel.Warn,
                    "Error" => LogLevel.Error,
                    _ => null
                };

                var entries = LoggingManager.Instance.ReadLogs(
                    _startDate,
                    _endDate,
                    string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                    minLevel,
                    5000
                );

                _logEntries.Clear();
                foreach (var entry in entries)
                {
                    _logEntries.Add(entry);
                }

                RaisePropertyChanged(nameof(LogEntries));
                RaisePropertyChanged(nameof(TotalEntries));

                StatusMessage = $"Found {entries.Count} entries";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void Refresh()
        {
            LoadLogFiles();
            Search();
        }

        private void ClearOldLogs()
        {
            try
            {
                var result = MessageBoxHelper.Show(
                    "Clear Old Logs",
                    "This will delete all log files except today's. Continue?",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning
                );

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var deleted = LoggingManager.Instance.ClearOldLogs();
                    StatusMessage = $"Deleted {deleted} old log file(s)";
                    Refresh();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing logs: {ex.Message}";
            }
        }

        private void OpenFolder()
        {
            try
            {
                if (System.IO.Directory.Exists(LogDirectory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = LogDirectory,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening folder: {ex.Message}";
            }
        }

        #endregion
    }
}

