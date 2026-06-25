using System;
using System.Collections.ObjectModel;
using SystemLib.Core;
using SystemLib.Services;

namespace VisionInspect.Services
{
    /// <summary>
    /// UI 日志收集服务 - 包装 NLogAdapter，同时将日志条目收集到 ObservableCollection 供 UI 绑定
    /// </summary>
    public class LogService : ILogger
    {
        private readonly NLogAdapter _nlog;
        private readonly ObservableCollection<LogEntryViewModel> _entries = new();
        private const int MaxEntries = 500;

        public LogService()
        {
            _nlog = new NLogAdapter("VisionInspect");
        }

        public ObservableCollection<LogEntryViewModel> Entries => _entries;

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Fatal(string message, Exception exception = null) => Log(LogLevel.Fatal, message, exception);

        private void Log(LogLevel level, string message, Exception ex = null)
        {
            // 写入 NLog
            switch (level)
            {
                case LogLevel.Trace: _nlog.Trace(message); break;
                case LogLevel.Debug: _nlog.Debug(message); break;
                case LogLevel.Info: _nlog.Info(message); break;
                case LogLevel.Warning: _nlog.Warning(message); break;
                case LogLevel.Error: _nlog.Error(message, ex); break;
                case LogLevel.Fatal: _nlog.Fatal(message, ex); break;
            }

            // 收集到 UI 列表
            var entry = new LogEntryViewModel
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = ex != null ? $"{message} ({ex.Message})" : message
            };

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _entries.Add(entry);
                while (_entries.Count > MaxEntries)
                    _entries.RemoveAt(0);
            });
        }
    }

    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string LevelText => Level.ToString().ToUpper();
        public string TimeText => Timestamp.ToString("HH:mm:ss.fff");
    }
}