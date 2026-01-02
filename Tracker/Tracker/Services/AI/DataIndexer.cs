using System.IO;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Main coordinator for indexing all user data as vectors
    /// </summary>
    public class DataIndexer
    {
        #region Singleton

        private static readonly Lazy<DataIndexer> _instance = 
            new(() => new DataIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static DataIndexer Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private bool _isIndexing = false;
        private DateTime _lastIndexed = DateTime.MinValue;
        private static readonly string LAST_INDEXED_FILE = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tracker", "LastIndexed.txt");

        #endregion

        #region Events

        public event EventHandler<IndexProgressEventArgs>? ProgressChanged;

        #endregion

        #region Constructor

        private DataIndexer()
        {
            _logger = LoggingManager.GetComponentLogger("DataIndexer");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Indexes all user data in the database
        /// </summary>
        public async Task<DataIndexStats> IndexAllDataAsync()
        {
            if (_isIndexing)
            {
                _logger.Warn("Indexing already in progress");
                return new DataIndexStats();
            }

            _isIndexing = true;
            var stats = new DataIndexStats();
            var startTime = DateTime.Now;

            try
            {
                // Load last indexed time
                LoadLastIndexedTime();
                DateTime? sinceTime = _lastIndexed == DateTime.MinValue ? null : _lastIndexed;
                
                if (sinceTime == null)
                {
                    _logger.Info("Starting full data indexing (first time)...");
                    RaiseProgress("Starting full indexing...");
                }
                else
                {
                    _logger.Info("Starting incremental indexing since {0}...", sinceTime.Value.ToString("g"));
                    RaiseProgress("Checking for changes...");
                }

                // Index team members (incremental)
                RaiseProgress("Indexing team members...");
                stats.TeamMembers = await TeamMemberIndexer.Instance.IndexAllAsync(sinceTime);

                // Index meetings (incremental)
                RaiseProgress("Indexing meetings...");
                stats.Meetings = await MeetingIndexer.Instance.IndexAllAsync(sinceTime);

                // Index tasks (incremental)
                RaiseProgress("Indexing tasks...");
                stats.Tasks = await TaskIndexer.Instance.IndexAllAsync(sinceTime);

                // Index OKRs, KPIs, Projects (incremental)
                RaiseProgress("Indexing goals and projects...");
                stats.Goals = await GoalIndexer.Instance.IndexAllAsync(sinceTime);

                // Index Pulse Surveys (incremental)
                RaiseProgress("Indexing pulse surveys...");
                stats.PulseSurveys = await PulseSurveyIndexer.Instance.IndexAllAsync(sinceTime);

                stats.TotalIndexed = stats.TeamMembers + stats.Meetings + stats.Tasks + stats.Goals + stats.PulseSurveys;
                stats.Duration = DateTime.Now - startTime;
                _lastIndexed = DateTime.Now;
                SaveLastIndexedTime();

                if (sinceTime == null)
                    _logger.Info("Full indexing complete: {0} total entities in {1:F1}s", 
                        stats.TotalIndexed, stats.Duration.TotalSeconds);
                else
                    _logger.Info("Incremental indexing complete: {0} entities updated in {1:F1}s", 
                        stats.TotalIndexed, stats.Duration.TotalSeconds);

                RaiseProgress(stats.TotalIndexed > 0 ? $"✓ Indexed {stats.TotalIndexed} entities" : "✓ No changes detected");

                return stats;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during data indexing");
                RaiseProgress("Error during indexing");
                return stats;
            }
            finally
            {
                _isIndexing = false;
            }
        }

        /// <summary>
        /// Checks if indexing is needed (never indexed or data changed)
        /// </summary>
        public bool ShouldReindex()
        {
            // If never indexed, yes
            if (_lastIndexed == DateTime.MinValue)
                return true;

            // If indexed more than 24 hours ago, yes
            if ((DateTime.Now - _lastIndexed).TotalHours > 24)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the last time data was indexed
        /// </summary>
        public DateTime GetLastIndexedTime() => _lastIndexed;

        #endregion

        #region Private Methods

        private void RaiseProgress(string message)
        {
            ProgressChanged?.Invoke(this, new IndexProgressEventArgs(message));
        }

        private void LoadLastIndexedTime()
        {
            try
            {
                if (File.Exists(LAST_INDEXED_FILE))
                {
                    var content = File.ReadAllText(LAST_INDEXED_FILE);
                    if (DateTime.TryParse(content, out var parsedTime))
                    {
                        _lastIndexed = parsedTime;
                        _logger.Info("Loaded last indexed time: {0}", _lastIndexed.ToString("g"));
                    }
                }
                else
                {
                    _lastIndexed = DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading last indexed time");
                _lastIndexed = DateTime.MinValue;
            }
        }

        private void SaveLastIndexedTime()
        {
            try
            {
                var directory = Path.GetDirectoryName(LAST_INDEXED_FILE);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(LAST_INDEXED_FILE, _lastIndexed.ToString("O"));
                _logger.Info("Saved last indexed time: {0}", _lastIndexed.ToString("g"));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving last indexed time");
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Statistics about the data indexing operation
    /// </summary>
    public class DataIndexStats
    {
        public int TeamMembers { get; set; }
        public int Meetings { get; set; }
        public int Tasks { get; set; }
        public int Goals { get; set; }
        public int PulseSurveys { get; set; }
        public int TotalIndexed { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event args for indexing progress updates
    /// </summary>
    public class IndexProgressEventArgs : EventArgs
    {
        public string Message { get; }
        
        public IndexProgressEventArgs(string message)
        {
            Message = message;
        }
    }

    #endregion
}
