using System.Collections.Generic;
using System.Threading.Tasks;

namespace SystemLib.Core
{
    /// <summary>
    /// 数据库操作接口
    /// </summary>
    public interface IDatabaseOperator
    {
        Task<int> ExecuteAsync(string sql, object param = null);
        Task<T> QuerySingleAsync<T>(string sql, object param = null);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);
        Task<int> InsertAsync(string table, object data);
        Task<int> UpdateAsync(string table, object data, string where);
    }
}
