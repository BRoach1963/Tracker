using Tracker.Common.Enums;

namespace Tracker.Logging
{
    public interface ILogger
    {
        void Debug(string msg, params object[] args);
        void Info(string msg, params object[] args);
        void Warn(string msg, params object[] args);
        void Error(string msg, params object[] args);
        void Exception(Exception ex, string msg, params object[] args);
        void SetGlobalHeaderValue(LogHeaderItem item, string value);
        string GetGlobalHeaderValue(LogHeaderItem item);
        LogLevel LogLevel { get; set; }
    }
}
