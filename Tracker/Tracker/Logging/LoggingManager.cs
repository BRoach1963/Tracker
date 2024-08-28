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

        private static volatile LoggingManager? _instance;
        private static readonly object SyncRoot = new object(); 

        public static LoggingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggingManager();
                        }
                    }
                }
                return _instance;
            }
        }

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        private LoggingManager()
        {
            _logQueue = new ConcurrentQueue<StrongBox<LogEntry>>();
            _logMessagePending = new AutoResetEvent(false);

            var logFileName = "Tracker.log"; 
            _logDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Tracker\\Logs\\";  
            _logFileFullName = $"{_logDirectory}{logFileName}";  

            _logFileMutex = new Mutex(false, "TrackerLogMutex");

            // create logger for core components
            _logger = new Logger("Core", _logFileFullName, GetLogLevel("Core"));
            _loggers.Add("Core", _logger);  

            LoadConfiguration();
            _cancellationTokenSource = new CancellationTokenSource();

            _writeThread = new Thread(() => WriteThread(_cancellationTokenSource.Token)) { Name = "LogWriteThread", IsBackground = true };
            _writeThread.Start();

            _perfMonThread = new Thread(() => PerformanceMonitorThread(_cancellationTokenSource.Token)) { Name = "PerformanceMonitorThread", IsBackground = true };
            _perfMonThread.Start();

            // print column headers into log for readability
            Write("Date|Time|LogLevel|Component|ThreadId|Cpu|Ram|vRam|ClrMem|LOH|Message");

            Write(Logger.Format(LogLevel.Info, null, "Core", "Tracker Application {0} is starting, Version {1}",
                Environment.Is64BitProcess ? "x64" : "x86", VersionHelper.GetAppVersion()));
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
                                    NotificationManager.Instance.SendToast(ToastNotificationAction.StatsWarningSystemBusy);
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

                //DateTime now = DateTime.Now;
                //if (_startTime.DayOfYear != now.DayOfYear)
                //{
                //    ArchiveLogs();
                //    _startTime = now;
                //}

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
                    // ReSharper disable once RedundantAssignment
                    setCreationTime = false;
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
}
