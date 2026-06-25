using System;
using System.Collections.Generic;
using System.Linq;
using SystemLib.Core;

namespace SystemLib.Services
{
    public class AlarmService : IAlarmManager
    {
        private readonly List<AlarmInfo> _activeAlarms = new List<AlarmInfo>();
        private readonly List<AlarmInfo> _history = new List<AlarmInfo>();
        private readonly object _lock = new object();

        public event Action<AlarmInfo> AlarmRaised;
        public event Action<string> AlarmCleared;

        public void Raise(string message, AlarmLevel level = AlarmLevel.Error, IReadOnlyDictionary<string, object> data = null)
        {
            var alarm = new AlarmInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                Message = message,
                Level = level,
                Timestamp = DateTime.Now,
                Data = data
            };

            lock (_lock)
            {
                _activeAlarms.Add(alarm);
                _history.Add(alarm);
            }

            AlarmRaised?.Invoke(alarm);
        }

        public void Clear(string alarmId)
        {
            lock (_lock)
            {
                var alarm = _activeAlarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm != null)
                {
                    _activeAlarms.Remove(alarm);
                }
            }

            AlarmCleared?.Invoke(alarmId);
        }

        public IReadOnlyList<AlarmInfo> GetActiveAlarms()
        {
            lock (_lock)
            {
                return _activeAlarms.ToList();
            }
        }

        public IReadOnlyList<AlarmInfo> GetHistory()
        {
            lock (_lock)
            {
                return _history.ToList();
            }
        }
    }
}