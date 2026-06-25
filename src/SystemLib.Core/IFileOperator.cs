using System.Threading.Tasks;

namespace SystemLib.Core
{
    /// <summary>
    /// 文件操作接口
    /// </summary>
    public interface IFileOperator
    {
        Task<byte[]> ReadAllBytesAsync(string path);
        Task WriteAllBytesAsync(string path, byte[] data);
        Task<string> ReadAllTextAsync(string path);
        Task WriteAllTextAsync(string path, string content);
        bool Exists(string path);
        void Delete(string path);
    }
}
