# API 接口文档

**产品**：通用视觉检测平台 | **版本**：V1.0.0

---

## 一、IImageProcessor — 图像处理算子接口

### 接口定义

```csharp
namespace ImageLib.Core
{
    public interface IImageProcessor
    {
        string Id { get; }
        string Name { get; }
        string Category { get; }
        IReadOnlyList<ParamDef> InputParams { get; }
        IReadOnlyList<ParamDef> OutputParams { get; }
        Task<ProcessResult> ExecuteAsync(ProcessContext ctx, CancellationToken ct);
    }
}
```

### 成员说明

| 成员 | 类型 | 说明 |
|------|------|------|
| Id | string | 唯一标识，如 "BlobAnalysis" |
| Name | string | 显示名称，如 "Blob分析" |
| Category | string | 分类：采集/预处理/定位/检测/测量/工具 |
| InputParams | IReadOnlyList\<ParamDef\> | 输入参数定义列表 |
| OutputParams | IReadOnlyList\<ParamDef\> | 输出参数定义列表 |
| ExecuteAsync | 方法 | 执行算子，返回 ProcessResult |

### 实现示例

```csharp
[Processor("Blob分析", Category = "检测")]
public class BlobAnalysisProcessor : IImageProcessor
{
    public string Id => "BlobAnalysis";
    public string Name => "Blob分析";
    public string Category => "检测";
    
    public IReadOnlyList<ParamDef> InputParams => new[]
    {
        new ParamDef("MinArea", ParamType.Double, 100, "最小面积"),
        new ParamDef("MaxArea", ParamType.Double, 999999, "最大面积"),
        new ParamDef("Threshold", ParamType.Double, 128, "分割阈值"),
    };
    
    public IReadOnlyList<ParamDef> OutputParams => new[]
    {
        new ParamDef("Defects", ParamType.Region, null, "缺陷区域"),
        new ParamDef("Count", ParamType.Double, null, "缺陷数量"),
    };
    
    public async Task<ProcessResult> ExecuteAsync(ProcessContext ctx, CancellationToken ct)
    {
        // 实现
    }
}
```

---

## 二、ICommunicationAdapter — 通讯适配器接口

### 接口定义

```csharp
namespace CommLib.Core
{
    public interface ICommunicationAdapter
    {
        string ProtocolName { get; }
        Task<ConnectionResult> ConnectAsync(ConnectionConfig config);
        Task<ConnectionResult> DisconnectAsync();
        Task<CommunicationResult> SendAsync(byte[] data);
        Task<CommunicationResult<string>> ReadAsync();
        ConnectionState State { get; }
    }
}
```

### 成员说明

| 成员 | 类型 | 说明 |
|------|------|------|
| ProtocolName | string | 协议名称，如 "Modbus TCP" |
| ConnectAsync | 方法 | 建立连接 |
| DisconnectAsync | 方法 | 断开连接 |
| SendAsync | 方法 | 发送数据 |
| ReadAsync | 方法 | 读取数据 |
| State | ConnectionState | 当前连接状态 |

---

## 三、IDevice — 设备接口

### 接口定义

```csharp
namespace CommLib.Core
{
    public interface IDevice
    {
        string Id { get; }
        string Name { get; }
        DeviceState State { get; }
        Task ConnectAsync();
        Task DisconnectAsync();
    }
}
```

---

## 四、ICamera — 相机接口

### 接口定义

```csharp
public interface ICamera : IDevice
{
    Task<ImageData> CaptureAsync();
    Task StartContinuousAsync(Action<ImageData> onFrame);
    Task StopContinuousAsync();
    void SetExposure(double us);
    void SetGain(double gain);
}
```

---

## 五、IRecipeExecutor — 流程执行器接口

### 接口定义

```csharp
namespace WorkflowEngine.Core
{
    public interface IRecipeExecutor
    {
        Task<RecipeResult> ExecuteAsync(InspectionRecipe recipe, CancellationToken ct);
    }
}
```

---

## 六、数据模型

### ImageData

```csharp
public class ImageData : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public string PixelFormat { get; }
    internal HObject HalconImage { get; }  // 不对外暴露
    public void Dispose();  // 归还对象池
}
```

### ProcessContext

```csharp
public class ProcessContext
{
    public Dictionary<string, ImageData> Images { get; }
    public Dictionary<string, RegionData> Regions { get; }
    public Dictionary<string, double> Numbers { get; }
    public Dictionary<string, string> Strings { get; }
    public Dictionary<string, object> Parameters { get; }
    public T GetParam<T>(string key, T defaultValue = default);
    public void SetOutput(string key, object value);
}
```

### ProcessResult

```csharp
public class ProcessResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, RegionData> OutputRegions { get; }
    public Dictionary<string, double> OutputNumbers { get; }
    public Dictionary<string, string> OutputStrings { get; }
}
```

### InspectionRecipe

```csharp
public class InspectionRecipe
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }  // 流程文件版本号
    public List<InspectionStep> Steps { get; set; }
}
```

### InspectionStep

```csharp
public class InspectionStep
{
    public string Id { get; set; }
    public string Name { get; set; }
    public StepType Type { get; set; }
    public List<ProcessorNode> SubSteps { get; set; }
    public Dictionary<string, object> Config { get; set; }
    public bool StopOnFailure { get; set; } = true;
}
```

### ProcessorNode

```csharp
public class ProcessorNode
{
    public string Id { get; set; }
    public string ProcessorType { get; set; }  // 对应 IImageProcessor.Id
    public Dictionary<string, object> Parameters { get; set; }
    public string InputReference { get; set; }
}
```

---

## 七、枚举类型

### StepType

| 值 | 说明 |
|----|------|
| ImageAcquisition | 图像采集 |
| Alignment | 定位/对位 |
| Inspection | 检测 |
| Measurement | 测量 |
| Communication | 通讯 |
| SystemOperation | 系统操作 |

### ConnectionState

| 值 | 说明 |
|----|------|
| Disconnected | 已断开 |
| Connecting | 连接中 |
| Connected | 已连接 |
| Reconnecting | 重连中 |

### DeviceState

| 值 | 说明 |
|----|------|
| Disconnected | 已断开 |
| Connected | 已连接 |
| Error | 错误状态 |
| Busy | 忙碌中 |