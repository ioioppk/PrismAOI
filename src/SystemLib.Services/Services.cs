using System;
using System.IO;
using System.Threading.Tasks;
using SystemLib.Core;

namespace SystemLib.Services
{
    public class NLogAdapter : ILogger
    {
        private readonly NLog.Logger _logger;

        public NLogAdapter(string name = "Default")
        {
            _logger = NLog.LogManager.GetLogger(name);
        }

        public void Trace(string message)
        {
            _logger.Trace(message);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warning(string message)
        {
            _logger.Warn(message);
        }

        public void Error(string message, Exception exception = null)
        {
            _logger.Error(exception, message);
        }

        public void Fatal(string message, Exception exception = null)
        {
            _logger.Fatal(exception, message);
        }
    }

    public class FileService : IFileOperator
    {
        public Task<byte[]> ReadAllBytesAsync(string path)
        {
            return Task.Run(() => File.ReadAllBytes(path));
        }

        public Task WriteAllBytesAsync(string path, byte[] data)
        {
            return Task.Run(() => File.WriteAllBytes(path, data));
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }

        public Task WriteAllTextAsync(string path, string content)
        {
            return Task.Run(() => File.WriteAllText(path, content));
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}