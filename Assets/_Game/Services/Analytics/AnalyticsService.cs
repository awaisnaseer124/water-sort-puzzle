using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorSort.Services.Analytics
{
    public interface IAnalyticsService
    {
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null);
    }

    public static class AnalyticsEvents
    {
        public const string LevelStart = "level_start";
        public const string LevelComplete = "level_complete";
        public const string LevelFail = "level_fail";
        public const string Ad = "ad";
        public const string Purchase = "purchase";
        public const string Hint = "hint";
        public const string Undo = "undo";
    }

    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
        }
    }

    public sealed class LogAnalyticsService : IAnalyticsService
    {
        private readonly Action<string> _write;

        public LogAnalyticsService(Action<string> write)
        {
            _write = write ?? throw new ArgumentNullException(nameof(write));
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            string details = parameters == null || parameters.Count == 0
                ? ""
                : " " + string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
            _write($"[Analytics] {eventName}{details}");
        }
    }
}
