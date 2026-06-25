using System;
using System.Threading;
using System.Threading.Tasks;

namespace ImageLib.Core
{
    /// <summary>
    /// 相机接口
    /// </summary>
    public interface ICamera : IDevice
    {
        Task<IImageData> CaptureAsync(CancellationToken ct = default);
        Task StartContinuousAsync(Action<IImageData> onFrame, CancellationToken ct = default);
        Task StopContinuousAsync();

        void SetExposure(double us);
        void SetGain(double gain);
    }
}
