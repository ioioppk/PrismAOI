using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommLib.Core;

namespace CommLib.WebApi
{
    public class WebApiAdapter : ICommunicationAdapter
    {
        private HttpClient _httpClient;
        private ConnectionConfig _config;
        private string _baseUrl;
        private string _apiKey;

        public string ProtocolName => "WebAPI";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public async Task<ConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            State = ConnectionState.Connecting;

            try
            {
                _baseUrl = config.Extra.TryGetValue("BaseUrl", out var url) ? url.TrimEnd('/') : "http://localhost:5000";
                _apiKey = config.Extra.TryGetValue("ApiKey", out var key) ? key : string.Empty;

                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs);
                _httpClient.BaseAddress = new Uri(_baseUrl);

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
                }

                // Test connectivity with a simple ping
                var response = await _httpClient.GetAsync("/api/ping");
                if (!response.IsSuccessStatusCode)
                {
                    State = ConnectionState.Error;
                    return ConnectionResult.Failure($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }

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
                    _httpClient?.Dispose();
                    _httpClient = null;
                    _baseUrl = null;
                    _apiKey = null;
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
                if (_httpClient == null)
                    return CommunicationResult.Failure("WebAPI 未连接");

                string sendEndpoint = _config.Extra.TryGetValue("SendEndpoint", out var se) ? se : "/api/data";
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.PostAsync(sendEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    return CommunicationResult.Failure($"发送失败: HTTP {(int)response.StatusCode}");
                }

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
                if (_httpClient == null)
                    return CommunicationResult<string>.Failure("WebAPI 未连接");

                string readEndpoint = _config.Extra.TryGetValue("ReadEndpoint", out var re) ? re : "/api/data";
                var response = await _httpClient.GetAsync(readEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    return CommunicationResult<string>.Failure($"读取失败: HTTP {(int)response.StatusCode}");
                }

                string result = await response.Content.ReadAsStringAsync();
                return CommunicationResult<string>.Success(result);
            }
            catch (Exception ex)
            {
                return CommunicationResult<string>.Failure(ex.Message);
            }
        }
    }
}