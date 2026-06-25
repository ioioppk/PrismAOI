using System.Collections.Generic;
using ImageLib.Core;

namespace WorkflowEngine.Core
{
    /// <summary>
    /// 步骤类型枚举
    /// </summary>
    public enum StepType
    {
        ImageAcquisition,   // 图像采集
        Alignment,         // 定位/对位
        Inspection,        // 检测
        Measurement,       // 测量
        Communication,     // 通讯
        SystemOperation    // 系统操作
    }

    /// <summary>
    /// 最细粒度：算子节点
    /// </summary>
    public class ProcessorNode
    {
        public string Id { get; set; }
        public string ProcessorType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string InputReference { get; set; }
    }

    /// <summary>
    /// 步骤接口
    /// </summary>
    public interface IRecipeStep
    {
        string Id { get; }
        string Name { get; }
        StepType Type { get; }
        List<ProcessorNode> SubSteps { get; }
        bool StopOnFailure { get; set; }
    }

    /// <summary>
    /// 步骤实现
    /// </summary>
    public class InspectionStep : IRecipeStep
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public StepType Type { get; set; }
        public List<ProcessorNode> SubSteps { get; set; } = new List<ProcessorNode>();
        public bool StopOnFailure { get; set; } = true;
    }

    /// <summary>
    /// 步骤执行结果
    /// </summary>
    public class StepResult
    {
        public string StepName { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public double ElapsedMs { get; set; }
        public Dictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();
    }
}
