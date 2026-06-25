using System.Threading.Tasks;

namespace ImageLib.Core
{
    /// <summary>
    /// 光源控制器接口
    /// </summary>
    public interface ILightController : IDevice
    {
        Task SetChannelAsync(int channel, int value);
        Task SetAllAsync(int[] values);
    }
}
