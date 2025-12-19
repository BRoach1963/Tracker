using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Tracker.Common.Enums;
using System.IO;
using System.Diagnostics;
using Tracker.Classes;
using Tracker.Helpers;
using Tracker.Managers;

namespace Tracker.Logging
{
    public partial class LoggingManager
    {
        private class LogEntry
        {
            public LogEntry(string value)
            {
                Value = value;
            }

            public string Value { get; private set; }
        }

        #region Singleton Instance

        private static readonly Lazy<LoggingManager> _lazyInstance = 
            new(() => new LoggingManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of LoggingManager.
        /// </summary>
        public static LoggingManager Instance => _lazyInstance.Value;

        #endregion

        #region Delegates

        public delegate void SystemStatsUpdatedDelegate(SystemStats stats);
        public SystemStatsUpdatedDelegate? OnSystemStatsUpdated { get; set; }

        #endregion

        #region Public Perf Metrics

        /// <summary>
        /// Uses Performance Counter and number of processors to return relative app CPU
        /// </summary> 

        public double CurrentAppMemory => GetCurrentMemoryUsage();

        #endregion

        /// <summary>
        /// Number of days to keep log files before cleanup.
        /// </summary>
        public const int LogRetentionDays = 7;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private LoggingManager()
        {
            _logQueue = new ConcurrentQueue<StrongBox<LogEntry>>();
            _logMessagePending = new AutoResetEvent(false);

            _logDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Tracker\\Logs\\";
            _currentLogDate = DateTime.Today;
            _logFileFullName = GetLogFilePathForDate(_currentLogDate);

            _logFileMutex = new Mutex(false, "TrackerLogMutex");

            // create logger for core components
            _logger = new Logger("Core", _logFileFullName, GetLogLevel("Core"));
            _loggers.Add("Core", _logger);  

            LoadConfiguration();
            _cancellationTokenSource = new CancellationTokenSource();

            // Clean up old log files on startup
            CleanupOldLogFiles();

            _writeThread = new Thread(() => WriteThread(_cancellationTokenSource.Token)) { Name = "LogWriteThread", IsBackground = true };
            _writeThread.Start();

            _perfMonThread = new Thread(() => PerformanceMonitorThread(_cancellationTokenSource.Token)) { Name = "PerformanceMonitorThread", IsBackground = true };
            _perfMonThread.Start();

            // print column headers into log for readability
            Write("Date|Time|LogLevel|Component|ThreadId|Cpu|Ram|vRam|ClrMem|LOH|Message");

            Write(Logger.Format(LogLevel.Info, null, "Core", "Tracker Application {0} is starting, Version {1}",
                Environment.Is64BitProcess ? "x64" : "x86", VersionHelper.GetAppVersion()));
        }

        /// <summary>
        /// Gets the log file path for a specific date.
        /// </summary>
        private string GetLogFilePathForDate(DateTime date)
        {
            return $"{_logDirectory}Tracker_{date:yyyy-MM-dd}.log";
        }

        private void LoadConfiguration()
        {
            var configFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Tracker\\Logs\\Logger.config";

            // no config, bail
            if (!File.Exists(configFile)) return;

            using var reader = new StreamReader(configFile);

            while (reader.ReadLine() is { } line)
            {
                try
                {
                    var configEntry = line.Split(',');

                    _logConfig[configEntry[0]] = (LogLevel)Convert.ToInt16(configEntry[1]);
                }
                catch { /* ignore invalid config */ }
            }
        }

        private double GetCurrentMemoryUsage()
        {
            var workingSet = Process.GetCurrentProcess().WorkingSet64;
            return Math.Round((double)workingSet / 1000 / 1000, 3);
        }

        private LogLevel GetLogLevel(string comp)
        {
            if (_logConfig.TryGetValue(comp, out var configuredLevel))
            {
                return configuredLevel;
            }

            return LogLevel.Info;
        }

        public static ILogger GetComponentLogger(string comp)
        {
            return LoggingManager.Instance.GetLogger(comp);
        }

        private ILogger GetLogger(string comp)
        {
            Logger? logger;

            lock (_loggers)
            {
                if (_loggers.TryGetValue(comp, out logger) == false)
                {
                    var level = GetLogLevel(comp);

                    logger = new Logger(comp, _logFileFullName, level);
                    _loggers.Add(comp, logger);
                }
            }

            return logger;
        }

        private void Write(string entry)
        {
            _logQueue.Enqueue(new StrongBox<LogEntry>(new LogEntry(entry)));
            _logMessagePending.Set();
        }

        private void PerformanceMonitorThread(CancellationToken token)
        {
            try
            {
                var p = Process.GetCurrentProcess();

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("% Processor Time", "Processor"))
                    {
                        PerformanceCounterCategory.Create("Processor",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "% Processor Time",
                            "CPU Usage by System");
                    }

                    _totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                catch
                {
                    _logger.Warn("Unable to create % Processor Time (System) performance counter");
                }

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("% Processor Time", "Process"))
                    {
                        PerformanceCounterCategory.Create("Process",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "% Processor Time",
                            "CPU Usage by process");
                    }

                    _cpu = new PerformanceCounter("Process", "% Processor Time", p.ProcessName, true);
                }
                catch
                {
                    _logger.Warn("Unable to create % Processor Time performance counter");
                }

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("Private Bytes", "Process"))
                    {
                        PerformanceCounterCategory.Create("Process",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "Private Bytes",
                            "Private bytes used by process");
                    }

                    _ram = new PerformanceCounter("Process", "Private Bytes", p.ProcessName, true);
                }
                catch
                {
                    _logger.Warn("Unable to create Private Bytes performance counter");
                }

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("Virtual Bytes", "Process"))
                    {
                        PerformanceCounterCategory.Create("Process",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "Virtual Bytes",
                            "Virtual bytes used by process");
                    }

                    _virtualRam = new PerformanceCounter("Process", "Virtual Bytes", p.ProcessName, true);
                }
                catch
                {
                    _logger.Warn("Unable to create Virtual Bytes performance counter");
                }

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("# Bytes in all Heaps", ".NET CLR Memory"))
                    {
                        PerformanceCounterCategory.Create(".NET CLR Memory",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "# Bytes in all Heaps",
                            "Bytes in all heaps");
                    }

                    _netClrMem = new PerformanceCounter(".NET CLR Memory", "# Bytes in all Heaps", p.ProcessName, true);
                }
                catch
                {
                    _logger.Warn("Unable to create # Bytes in all Heaps performance counter");
                }

                try
                {
                    if (!PerformanceCounterCategory.CounterExists("Large Object Heap size", ".NET CLR Memory"))
                    {
                        PerformanceCounterCategory.Create(".NET CLR Memory",
                            "Process counter",
                            PerformanceCounterCategoryType.SingleInstance,
                            "Large Object Heap size",
                            "Large object heap size");
                    }

                    _largeObjHeap = new PerformanceCounter(".NET CLR Memory", "Large Object Heap size", p.ProcessName, true);
                }
                catch
                {
                    _logger.Warn("Unable to create Large Object Heap size performance counter");
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _currentTotalCpu = _totalCpu != null ? _totalCpu.NextValue() : 0;

                        // get snapshot of current CPU being used, accounting for multiple processors
                        _currentCpu = _cpu != null ? _cpu.NextValue() / Environment.ProcessorCount : 0;

                        var currentCpu = $"{Math.Ceiling(_currentCpu)}%";

                        // get snapshot of current RAM being used
                        var currentRam = _ram != null ? $"{Math.Round(_ram.NextValue() / 1024 / 1024, 0)}MB" : string.Empty;

                        var currentVirtualRam = _virtualRam != null ? $"{Math.Round(_virtualRam.NextValue() / 1024 / 1024, 0)}MB" : string.Empty;

                        var currentNetClrMem = _netClrMem != null ? $"{Math.Round(_netClrMem.NextValue() / 1024 / 1024, 0)}MB" : string.Empty;

                        var largeObjHeap = _largeObjHeap != null ? $"{Math.Round(_largeObjHeap.NextValue() / 1024 / 1024, 0)}MB" : string.Empty; 

                        // update headers for all loggers
                        _logger.SetGlobalHeaderValue(LogHeaderItem.CpuPct, currentCpu);
                        _logger.SetGlobalHeaderValue(LogHeaderItem.Ram, currentRam);
                        _logger.SetGlobalHeaderValue(LogHeaderItem.VirtualRam, currentVirtualRam);
                        _logger.SetGlobalHeaderValue(LogHeaderItem.NetClrMem, currentNetClrMem);
                        _logger.SetGlobalHeaderValue(LogHeaderItem.LargeObjHeap, largeObjHeap); 

                        if (Win32UtilHelper.GetGlobalMemoryStats(out var memLoad, out var total, out var avail))
                        {
                            var totalMb = total / 1024 / 1024;
                            var availMb = avail / 1024 / 1024;

                            _logger.Debug("LogDeviceInfo: Current System Memory Load: {0}%, Total Memory (MB): {1}, Available Memory (MB): {2}",
                                memLoad, totalMb, availMb);
                            
                            // regardless of installed RAM, if there is less than 500MB of available RAM the system is starting to run
                            // dangerously close to out of memory - show warning message
                            // do not show if not in a meeting, as the temporary spike during app launch shouldn't be flagged
                            if (availMb < 500)
                            {
                                if (++_performanceHitCount > 5)
                                {
                                    _performanceHitCount = 0;
                                    NotificationManager.Instance.SendNativeToast(ToastNotificationAction.StatsWarningSystemBusy);
                                }
                            }
                            else
                            {
                                _performanceHitCount = 0;
                            }
                        } 

                        OnSystemStatsUpdated?.Invoke(new SystemStats(_currentCpu, _currentTotalCpu, memLoad, CurrentAppMemory));
                    }
                    catch
                    {
                        // swallow
                    }

                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                _logger.Exception(e, "Error in Performance Monitor Thread");
            }
        }

        private void WriteThread(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    WriteActiveLogs();

                    try
                    {
                        if (!_logMessagePending.WaitOne(10000))
                        {
                            // log heartbeat every 10 seconds if no other message comes in
                            _logger.Debug("Application Heartbeat");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Exception(ex, "Exception caught in LoggingManager WriteThread waiting on message pending event");
                    }
                }

                if (token.IsCancellationRequested)
                {
                    Write(Logger.Format(LogLevel.Info, null, "Core", "LoggingManager: WriteThread shutting down"));
                    WriteActiveLogs();
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Exception caught in LoggingManager WriteThread");
            }
        }

        private void WriteActiveLogs()
        {
            try
            {
                try
                {
                    _logFileMutex.WaitOne();
                }
                catch (AbandonedMutexException) { /* swallow */ }

                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Check for day rollover
                if (DateTime.Today != _currentLogDate)
                {
                    _currentLogDate = DateTime.Today;
                    _logFileFullName = GetLogFilePathForDate(_currentLogDate);
                    
                    // Update all loggers to use new file
                    foreach (var logger in _loggers.Values)
                    {
                        logger.UpdateLogFile(_logFileFullName);
                    }
                    
                    // Clean up old files
                    CleanupOldLogFiles();
                    
                    // Write header to new file
                    Write("Date|Time|LogLevel|Component|ThreadId|Cpu|Ram|vRam|ClrMem|LOH|Message");
                }

                bool setCreationTime = !File.Exists(_logFileFullName);

                using (var sw = File.AppendText(_logFileFullName))
                {
                    while (_logQueue.TryDequeue(result: out StrongBox<LogEntry>? logEntryWrapper))
                    {
                        sw.WriteLine(logEntryWrapper.Value?.Value);
                    }
                }

                if (setCreationTime)
                {
                    File.SetCreationTime(_logFileFullName, DateTime.Now);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // unrecoverable - rethrow
                throw;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "LoggingManager exception while writing to log file");
            }
            finally
            {
                try
                {
                    _logFileMutex.ReleaseMutex();
                }
                catch { /* swallow */ }
            }
        }

        /// <summary>
        /// Cleans up log files older than the retention period.
        /// </summary>
        private void CleanupOldLogFiles()
        {
            try
            {
                if (!Directory.Exists(_logDirectory)) return;

                var cutoffDate = DateTime.Today.AddDays(-LogRetentionDays);
                var logFiles = Directory.GetFiles(_logDirectory, "Tracker_*.log");

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        // Parse date from filename: Tracker_2025-12-11
                        if (fileName.Length >= 17 && DateTime.TryParse(fileName.Substring(8), out var fileDate))
                        {
                            if (fileDate < cutoffDate)
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    catch { /* ignore individual file errors */ }
                }
            }
            catch { /* ignore cleanup errors */ }
        }

        #region Public Log Access Methods

        /// <summary>
        /// Gets the log directory path.
        /// </summary>
        public string LogDirectory => _logDirectory;

        /// <summary>
        /// Gets all available log files, newest first.
        /// </summary>
        public List<LogFileInfo> GetLogFiles()
        {
            var result = new List<LogFileInfo>();
            
            try
            {
                if (!Directory.Exists(_logDirectory)) return result;

                var logFiles = Directory.GetFiles(_logDirectory, "Tracker_*.log")
                    .OrderByDescending(f => f);

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    DateTime? logDate = null;
                    
                    if (fileName.Length >= 17 && DateTime.TryParse(fileName.Substring(8), out var parsed))
                    {
                        logDate = parsed;
                    }

                    result.Add(new LogFileInfo
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        Date = logDate ?? fileInfo.CreationTime.Date,
                        SizeBytes = fileInfo.Length,
                        IsCurrentLog = file == _logFileFullName
                    });
                }
            }
            catch { /* return empty list on error */ }

            return result;
        }

        /// <summary>
        /// Reads log entries from a specific date range with optional text filter.
        /// </summary>
        public List<LogEntryParsed> ReadLogs(DateTime? startDate = null, DateTime? endDate = null, 
            string? searchText = null, LogLevel? minLevel = null, int maxEntries = 1000)
        {
            var result = new List<LogEntryParsed>();
            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today;

            try
            {
                var logFiles = GetLogFiles()
                    .Where(f => f.Date >= start.Date && f.Date <= end.Date)
                    .OrderBy(f => f.Date);

                foreach (var logFile in logFiles)
                {
                    if (result.Count >= maxEntries) break;

                    var entries = ReadLogFile(logFile.FilePath, searchText, minLevel, maxEntries - result.Count);
                    result.AddRange(entries);
                }
            }
            catch { /* return partial results on error */ }

            return result.OrderByDescending(e => e.Timestamp).ToList();
        }

        /// <summary>
        /// Reads entries from a specific log file.
        /// </summary>
        private List<LogEntryParsed> ReadLogFile(string filePath, string? searchText, LogLevel? minLevel, int maxEntries)
        {
            var result = new List<LogEntryParsed>();

            try
            {
                if (!File.Exists(filePath)) return result;

                // Read file with shared access (allows reading while logging)
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                string? line;
                while ((line = reader.ReadLine()) != null && result.Count < maxEntries)
                {
                    // Skip header line
                    if (line.StartsWith("Date|Time|")) continue;

                    var entry = ParseLogEntry(line);
                    if (entry == null) continue;

                    // Apply filters
                    if (minLevel.HasValue && entry.Level < minLevel.Value) continue;
                    if (!string.IsNullOrEmpty(searchText) && 
                        !entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                        !entry.Component.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    result.Add(entry);
                }
            }
            catch { /* return partial results */ }

            return result;
        }

        /// <summary>
        /// Parses a log line into a structured entry.
        /// Format: Date|Time|LogLevel|Component|ThreadId|Cpu|Ram|vRam|ClrMem|LOH|Message
        /// </summary>
        private LogEntryParsed? ParseLogEntry(string line)
        {
            try
            {
                var parts = line.Split('|');
                if (parts.Length < 11) return null;

                var dateStr = parts[0];
                var timeStr = parts[1];
                
                if (!DateTime.TryParse($"{dateStr} {timeStr}", out var timestamp))
                    return null;

                if (!Enum.TryParse<LogLevel>(parts[2], true, out var level))
                    level = LogLevel.Info;

                return new LogEntryParsed
                {
                    Timestamp = timestamp,
                    Level = level,
                    Component = parts[3],
                    ThreadId = parts[4],
                    Message = string.Join("|", parts.Skip(10)) // Message may contain |
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clears all log files except the current one.
        /// </summary>
        public int ClearOldLogs()
        {
            int deletedCount = 0;
            
            try
            {
                if (!Directory.Exists(_logDirectory)) return 0;

                var logFiles = Directory.GetFiles(_logDirectory, "Tracker_*.log");

                foreach (var file in logFiles)
                {
                    // Don't delete the current log file
                    if (file == _logFileFullName) continue;

                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch { /* ignore individual file errors */ }
                }
            }
            catch { /* ignore cleanup errors */ }

            return deletedCount;
        }

        /// <summary>
        /// Clears all log files including the current one (starts fresh).
        /// </summary>
        public void ClearAllLogs()
        {
            try
            {
                _logFileMutex.WaitOne();
                
                if (Directory.Exists(_logDirectory))
                {
                    var logFiles = Directory.GetFiles(_logDirectory, "Tracker_*.log");
                    foreach (var file in logFiles)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                // Write header to fresh log
                Write("Date|Time|LogLevel|Component|ThreadId|Cpu|Ram|vRam|ClrMem|LOH|Message");
                Write(Logger.Format(LogLevel.Info, null, "Core", "Logs cleared by user"));
            }
            finally
            {
                try { _logFileMutex.ReleaseMutex(); } catch { }
            }
        }

        #endregion

        public void Shutdown()
        {
            // already cleaned up
            if (_shuttingDown) return;

            _shuttingDown = true;
            _cancellationTokenSource.Cancel();

            _logMessagePending.Set();

            if (_writeThread != null)
            {
                if (!_writeThread.Join(1000))
                {
                    
                }
            }

            if (_perfMonThread != null)
            {
                if (!_perfMonThread.Join(500))
                { 
                }
            }
        }

        #region Members

        private Logger _logger;
        private Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
        private string _logDirectory;  
        private string _logFileFullName;
        private DateTime _currentLogDate;
        private Mutex _logFileMutex;

        private CancellationTokenSource _cancellationTokenSource;

        private Thread? _writeThread;
        private Thread? _perfMonThread;

        private ConcurrentQueue<StrongBox<LogEntry>> _logQueue;
        private AutoResetEvent _logMessagePending;
        private volatile bool _shuttingDown;

        private PerformanceCounter? _totalCpu;
        private PerformanceCounter? _cpu;
        private PerformanceCounter? _ram;
        private PerformanceCounter? _virtualRam;
        private PerformanceCounter? _netClrMem;
        private PerformanceCounter? _largeObjHeap;

        private double _currentCpu;
        private double _currentTotalCpu;

        private Dictionary<string, LogLevel> _logConfig = new Dictionary<string, LogLevel>(); 

        private int _performanceHitCount;

        #endregion
    }

    /// <summary>
    /// Information about a log file.
    /// </summary>
    public class LogFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public long SizeBytes { get; set; }
        public bool IsCurrentLog { get; set; }
        
        public string SizeFormatted => SizeBytes < 1024 ? $"{SizeBytes} B" :
                                        SizeBytes < 1024 * 1024 ? $"{SizeBytes / 1024.0:F1} KB" :
                                        $"{SizeBytes / 1024.0 / 1024.0:F1} MB";
    }

    /// <summary>
    /// A parsed log entry.
    /// </summary>
    public class LogEntryParsed
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Component { get; set; } = string.Empty;
        public string ThreadId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string LevelDisplay => Level.ToString().ToUpper();
        public string TimeDisplay => Timestamp.ToString("HH:mm:ss.fff");
        public string DateDisplay => Timestamp.ToString("yyyy-MM-dd");
    }
}
