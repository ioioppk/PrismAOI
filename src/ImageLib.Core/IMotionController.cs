using System.Threading.Tasks;

namespace ImageLib.Core
{
    /// <summary>
    /// 运动控制卡接口
    /// </summary>
    public interface IMotionController : IDevice
    {
        Task MoveAbsoluteAsync(int axis, double position);
        Task MoveRelativeAsync(int axis, double delta);
        Task HomeAsync(int axis);
        Task<double> GetPositionAsync(int axis);
    }
}
