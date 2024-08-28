using Tracker.Common.Enums;

namespace Tracker.Logging
{
    public partial class LoggingManager
    {
        internal class Logger : ILogger
        {
            #region Fields
            
            /// <summary>
            /// Use this array to add data points to the header of each log entry
            /// </summary>
            private static string[] _headerItems = new string[Enum.GetValues(typeof(LogHeaderItem)).Length];

            #endregion

            public Logger(string component, string logName, LogLevel level = LogLevel.Debug)
            {
                ComponentName = component;
                LogFileName = logName;
                LogLevel = level;
            }

            #region Properties

            public string ComponentName { get; private set; }

            public string LogFileName { get; private set; }

            public LogLevel LogLevel { get; set; }

            #endregion

            #region Methods

            public void Debug(string format, params object[] args)
            {
                if (LogLevel != LogLevel.None && LogLevel <= LogLevel.Debug)
                {
                    Write(LogLevel.Debug, null, format, args);
                }
            }

            public void Info(string format, params object[] args)
            {
                if (LogLevel != LogLevel.None && LogLevel <= LogLevel.Info)
                {
                    Write(LogLevel.Info, null, format, args);
                }
            }

            public void Warn(string format, params object[] args)
            {
                if (LogLevel != LogLevel.None && LogLevel <= LogLevel.Warn)
                {
                    Write(LogLevel.Warn, null, format, args);
                }
            }

            public void Error(string format, params object[] args)
            {
                if (LogLevel != LogLevel.None && LogLevel <= LogLevel.Error)
                {
                    Write(LogLevel.Error, null, format, args);
                }
            }

            public void Exception(Exception ex, string format, params object[] args)
            {
                if (LogLevel <= LogLevel.Error)
                {
                    Write(LogLevel.Error, ex, format, args);
                }
            }

            private void Write(LogLevel level, Exception? ex, string format, params object[] args)
            {
                string logEntry = Format(level, ex, ComponentName, format, args);

                LoggingManager.Instance.Write(logEntry);
            }
            public static string Format(LogLevel level, Exception? ex, string componentName, string format, params object[]? args)
            {
                // if parameterized string is provided, inject args into string
                string message = (args == null || args.Length == 0) ? format : string.Format(format, args);

                string logEntry = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}",
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    level.ToString().ToUpper(),
                    componentName,
                    Thread.CurrentThread.ManagedThreadId,
                    _headerItems[(int)LogHeaderItem.CpuPct],
                    _headerItems[(int)LogHeaderItem.Ram],
                    _headerItems[(int)LogHeaderItem.VirtualRam],
                    _headerItems[(int)LogHeaderItem.NetClrMem],
                    _headerItems[(int)LogHeaderItem.LargeObjHeap], 
                    message);

                if (ex != null)
                {
                    logEntry += $" (Exception={ex.Message}){Environment.NewLine}{ex.StackTrace}";
                }

                logEntry = logEntry.Replace("\n", "\n>");

                return logEntry;
            }

            public void SetGlobalHeaderValue(LogHeaderItem item, string value)
            {
                _headerItems[(int)item] = value;
            }

            public string GetGlobalHeaderValue(LogHeaderItem item)
            {
                try
                {
                    return _headerItems[(int)item];
                }
                catch
                {
                    return string.Empty;
                }
            }


            #endregion
        }
    }
}
