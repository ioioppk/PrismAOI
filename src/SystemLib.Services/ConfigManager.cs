using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SystemLib.Core;

namespace SystemLib.Services
{
    public class ConfigManager : IConfigManager
    {
        private readonly string _filePath;
        private readonly Dictionary<string, object> _config;
        private readonly object _lock = new object();

        public ConfigManager(string filePath = "config.json")
        {
            _filePath = filePath;
            _config = new Dictionary<string, object>();

            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    _config = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                              ?? new Dictionary<string, object>();
                }
                catch
                {
                    _config = new Dictionary<string, object>();
                }
            }
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            lock (_lock)
            {
                if (_config.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is T tValue)
                            return tValue;

                        // Handle JToken/JObject conversion
                        var serialized = JsonConvert.SerializeObject(value);
                        return JsonConvert.DeserializeObject<T>(serialized);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        public Task<T> GetAsync<T>(string key, T defaultValue = default)
        {
            return Task.FromResult(Get(key, defaultValue));
        }

        public void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                _config[key] = value;
            }
        }

        public Task SetAsync<T>(string key, T value)
        {
            Set(key, value);
            return Task.CompletedTask;
        }

        public void Save()
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(_filePath, json);
            }
        }

        public Task SaveAsync()
        {
            return Task.Run(() => Save());
        }

        public IReadOnlyDictionary<string, object> GetAll()
        {
            lock (_lock)
            {
                return new Dictionary<string, object>(_config);
            }
        }
    }
}