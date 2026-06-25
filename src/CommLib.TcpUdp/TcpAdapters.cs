using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommLib.Core;

namespace CommLib.TcpUdp
{
    public class TcpClientAdapter : ICommunicationAdapter
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private ConnectionConfig _config;

        public string ProtocolName => "TCP 客户端";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public async Task<ConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            State = ConnectionState.Connecting;

            try
            {
                string host = config.Extra.TryGetValue("Host", out var h) ? h : "127.0.0.1";
                int port = config.Extra.TryGetValue("Port", out var p) && int.TryParse(p, out var portVal) ? portVal : 8080;

                _tcpClient = new TcpClient();
                var connectTask = _tcpClient.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(config.TimeoutMs)) != connectTask)
                {
                    _tcpClient.Dispose();
                    _tcpClient = null;
                    State = ConnectionState.Timeout;
                    return ConnectionResult.Failure("连接超时");
                }

                await connectTask;
                _stream = _tcpClient.GetStream();
                _stream.ReadTimeout = config.TimeoutMs;
                _stream.WriteTimeout = config.TimeoutMs;

                State = ConnectionState.Connected;
                return ConnectionResult.Success();
            }
            catch (Exception ex)
            {
                State = ConnectionState.Error;
                return ConnectionResult.Failure(ex.Message);
            }
        }

        public Task<ConnectionResult> DisconnectAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _stream?.Close();
                    _stream = null;
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                    _tcpClient = null;
                    State = ConnectionState.Disconnected;
                    return ConnectionResult.Success();
                }
                catch (Exception ex)
                {
                    State = ConnectionState.Error;
                    return ConnectionResult.Failure(ex.Message);
                }
            });
        }

        public async Task<CommunicationResult> SendAsync(byte[] data)
        {
            try
            {
                if (_stream == null || _tcpClient == null || !_tcpClient.Connected)
                    return CommunicationResult.Failure("TCP 客户端未连接");

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                return CommunicationResult.Success();
            }
            catch (Exception ex)
            {
                return CommunicationResult.Failure(ex.Message);
            }
        }

        public async Task<CommunicationResult<string>> ReadAsync()
        {
            try
            {
                if (_stream == null || _tcpClient == null || !_tcpClient.Connected)
                    return CommunicationResult<string>.Failure("TCP 客户端未连接");

                var buffer = new byte[4096];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                string result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return CommunicationResult<string>.Success(result);
            }
            catch (Exception ex)
            {
                return CommunicationResult<string>.Failure(ex.Message);
            }
        }
    }

    public class TcpServerAdapter : ICommunicationAdapter
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private ConnectionConfig _config;

        public string ProtocolName => "TCP 服务器";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public async Task<ConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            State = ConnectionState.Connecting;

            try
            {
                string host = config.Extra.TryGetValue("Host", out var h) ? h : "127.0.0.1";
                int port = config.Extra.TryGetValue("Port", out var p) && int.TryParse(p, out var portVal) ? portVal : 8080;

                var ipAddress = IPAddress.Parse(host);
                _listener = new TcpListener(ipAddress, port);
                _listener.Start();

                var acceptTask = _listener.AcceptTcpClientAsync();
                if (await Task.WhenAny(acceptTask, Task.Delay(config.TimeoutMs)) != acceptTask)
                {
                    _listener.Stop();
                    _listener = null;
                    State = ConnectionState.Timeout;
                    return ConnectionResult.Failure("等待客户端连接超时");
                }

                _client = await acceptTask;
                _stream = _client.GetStream();
                _stream.ReadTimeout = config.TimeoutMs;
                _stream.WriteTimeout = config.TimeoutMs;

                State = ConnectionState.Connected;
                return ConnectionResult.Success();
            }
            catch (Exception ex)
            {
                State = ConnectionState.Error;
                return ConnectionResult.Failure(ex.Message);
            }
        }

        public Task<ConnectionResult> DisconnectAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _stream?.Close();
                    _stream = null;
                    _client?.Close();
                    _client?.Dispose();
                    _client = null;
                    _listener?.Stop();
                    _listener = null;
                    State = ConnectionState.Disconnected;
                    return ConnectionResult.Success();
                }
                catch (Exception ex)
                {
                    State = ConnectionState.Error;
                    return ConnectionResult.Failure(ex.Message);
                }
            });
        }

        public async Task<CommunicationResult> SendAsync(byte[] data)
        {
            try
            {
                if (_stream == null || _client == null || !_client.Connected)
                    return CommunicationResult.Failure("TCP 服务器未连接客户端");

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                return CommunicationResult.Success();
            }
            catch (Exception ex)
            {
                return CommunicationResult.Failure(ex.Message);
            }
        }

        public async Task<CommunicationResult<string>> ReadAsync()
        {
            try
            {
                if (_stream == null || _client == null || !_client.Connected)
                    return CommunicationResult<string>.Failure("TCP 服务器未连接客户端");

                var buffer = new byte[4096];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                string result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return CommunicationResult<string>.Success(result);
            }
            catch (Exception ex)
            {
                return CommunicationResult<string>.Failure(ex.Message);
            }
        }
    }
}