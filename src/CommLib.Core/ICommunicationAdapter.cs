using System;
using System.Threading.Tasks;

namespace CommLib.Core
{
    /// <summary>
    /// 通讯协议适配器接口
    /// </summary>
    public interface ICommunicationAdapter
    {
        string ProtocolName { get; }
        ConnectionState State { get; }

        Task<ConnectionResult> ConnectAsync(ConnectionConfig config);
        Task<ConnectionResult> DisconnectAsync();
        Task<CommunicationResult> SendAsync(byte[] data);
        Task<CommunicationResult<string>> ReadAsync();
    }

    /// <summary>
    /// 连接监控接口（心跳、自动重连）
    /// </summary>
    public interface IConnectionMonitor
    {
        bool IsConnected { get; }
        event Action Connected;
        event Action Disconnected;
        Task<bool> PingAsync();
    }

    /// <summary>
    /// 流式数据接口（高频采集）
    /// </summary>
    public interface IStreamAdapter : ICommunicationAdapter
    {
        IObservable<byte[]> DataStream { get; }
    }
}
