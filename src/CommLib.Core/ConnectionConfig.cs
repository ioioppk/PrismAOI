using System;
using System.Collections.Generic;

namespace CommLib.Core
{
    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Timeout
    }

    /// <summary>
    /// 连接配置基类
    /// </summary>
    public class ConnectionConfig
    {
        public string Name { get; set; } = string.Empty;
        public int TimeoutMs { get; set; } = 5000;
        public int RetryCount { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public Dictionary<string, string> Extra { get; set; } = new Dictionary<string, string>();
    }
}
