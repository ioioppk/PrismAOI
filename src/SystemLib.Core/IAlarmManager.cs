using System;
using System.Collections.Generic;

namespace SystemLib.Core
{
    /// <summary>
    /// 报警级别
    /// </summary>
    public enum AlarmLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 报警信息
    /// </summary>
    public class AlarmInfo
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public AlarmLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// 报警管理器接口
    /// </summary>
    public interface IAlarmManager
    {
        void Raise(string message, AlarmLevel level = AlarmLevel.Error, IReadOnlyDictionary<string, object> data = null);
        void Clear(string alarmId);
        event Action<AlarmInfo> AlarmRaised;
        event Action<string> AlarmCleared;
    }
}
