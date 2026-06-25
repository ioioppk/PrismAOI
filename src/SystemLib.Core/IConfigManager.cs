using System.Collections.Generic;
using System.Threading.Tasks;

namespace SystemLib.Core
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigManager
    {
        T Get<T>(string key, T defaultValue = default);
        Task<T> GetAsync<T>(string key, T defaultValue = default);
        void Set<T>(string key, T value);
        Task SetAsync<T>(string key, T value);
        void Save();
        Task SaveAsync();
        IReadOnlyDictionary<string, object> GetAll();
    }
}
