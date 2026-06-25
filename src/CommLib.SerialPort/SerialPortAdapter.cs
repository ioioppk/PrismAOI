using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using CommLib.Core;

namespace CommLib.SerialPort
{
    public class SerialPortAdapter : ICommunicationAdapter
    {
        private System.IO.Ports.SerialPort _serialPort;
        private ConnectionConfig _config;

        public string ProtocolName => "串口";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public async Task<ConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            State = ConnectionState.Connecting;

            try
            {
                string portName = config.Extra.TryGetValue("PortName", out var p) ? p : "COM1";
                int baudRate = config.Extra.TryGetValue("BaudRate", out var b) && int.TryParse(b, out var br) ? br : 9600;
                int dataBits = config.Extra.TryGetValue("DataBits", out var d) && int.TryParse(d, out var db) ? db : 8;
                Parity parity = Parity.None;
                if (config.Extra.TryGetValue("Parity", out var parityStr))
                {
                    if (!Enum.TryParse(parityStr, true, out parity))
                        parity = Parity.None;
                }
                StopBits stopBits = StopBits.One;
                if (config.Extra.TryGetValue("StopBits", out var sbStr))
                {
                    if (!Enum.TryParse(sbStr, true, out stopBits))
                        stopBits = StopBits.One;
                }

                await Task.Run(() =>
                {
                    _serialPort = new System.IO.Ports.SerialPort(portName, baudRate, parity, dataBits, stopBits)
                    {
                        ReadTimeout = config.TimeoutMs,
                        WriteTimeout = config.TimeoutMs
                    };
                    _serialPort.Open();
                });

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
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                    }
                    _serialPort = null;
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

        public Task<CommunicationResult> SendAsync(byte[] data)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                        return CommunicationResult.Failure("串口未打开");

                    _serialPort.Write(data, 0, data.Length);
                    return CommunicationResult.Success();
                }
                catch (TimeoutException)
                {
                    State = ConnectionState.Timeout;
                    return CommunicationResult.Failure("发送超时");
                }
                catch (Exception ex)
                {
                    return CommunicationResult.Failure(ex.Message);
                }
            });
        }

        public Task<CommunicationResult<string>> ReadAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                        return CommunicationResult<string>.Failure("串口未打开");

                    var buffer = new byte[4096];
                    int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
                    string result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return CommunicationResult<string>.Success(result);
                }
                catch (TimeoutException)
                {
                    State = ConnectionState.Timeout;
                    return CommunicationResult<string>.Failure("读取超时");
                }
                catch (Exception ex)
                {
                    return CommunicationResult<string>.Failure(ex.Message);
                }
            });
        }
    }
}