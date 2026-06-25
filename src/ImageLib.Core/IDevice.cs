using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageLib.Core
{
    /// <summary>
    /// 设备状态枚举
    /// </summary>
    public enum DeviceState
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Unknown
    }

    /// <summary>
    /// 基础设备接口
    /// </summary>
    public interface IDevice
    {
        string Id { get; }
        string Name { get; }
        DeviceState State { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
    }
}
