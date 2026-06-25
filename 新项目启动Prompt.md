# 新项目启动 Prompt

将此文件内容复制到新 AI 对话窗口，即可接续开发。

---

## 项目背景

我在开发一个**通用视觉检测平台**，面向半导体芯片检测行业。技术栈：WPF + Prism 8.x + Halcon 22.x + .NET 6.0。

## 当前阶段

**第一阶段：地基（第 1-4 周）**，当前处于第 1 周结束。

## 已完成

- [x] 项目目录结构（src/ 下 10 个子模块，tests/ 下 4 个测试项目）
- [x] 全部核心接口定义并冻结：
  - IImageProcessor（图像处理算子统一接口）
  - ICommunicationAdapter（通讯协议适配器接口）
  - IDevice / ICamera / ILightController / IMotionController
  - ILogger / IFileOperator / IConfigManager / IDatabaseOperator / IAlarmManager
  - IRecipeExecutor（流程执行器接口）
- [x] 核心数据模型：
  - ImageData（图像数据封装，Disposable + 对象池）
  - RegionData（区域数据封装）
  - ProcessContext（算子执行上下文，含输入/输出容器）
  - ProcessResult（算子执行结果）
  - InspectionRecipe / InspectionStep / ProcessorNode（流程定义模型）
  - ConnectionConfig / CommunicationResult / ConnectionResult（通讯模型）
  - ParamDef / ProcessorAttribute / ProcessorInfo（算子元数据）
- [x] HalconEngine 单例封装骨架（HalconBridge）
- [x] RecipeExecutor 顺序执行器实现
- [x] RecipeSerializer JSON 序列化/反序列化
- [x] ProcessorDiscovery 算子自动发现机制
- [x] 3 个基础算子示例：
  - AcquisitionProcessor（采集算子）
  - GrayScaleProcessor（灰度化算子）
  - BlobAnalysisProcessor（Blob 分析算子）
- [x] 2 个通讯适配器示例：
  - SerialPortAdapter（串口适配器）
  - ModbusTcpAdapter（Modbus TCP 适配器）
- [x] .editorconfig 代码规范
- [x] 单元测试示例（BlobAnalysisTests）

## 下一步任务（第 2 周）

请帮我完成以下任务：

1. **完善 HalconBridge**：HalconEngine 中的 TODO 替换为实际 Halcon API 调用（ReadImage、WriteImage、License 检查）

2. **完善 3 个算子的 Halcon 调用**：
   - AcquisitionProcessor：实现从文件读图
   - GrayScaleProcessor：实现 Rgb1ToGray
   - BlobAnalysisProcessor：实现 Threshold → Connection → SelectShape

3. **创建 .csproj 项目文件**（10 个核心项目 + 4 个测试项目），配置 NuGet 依赖

4. **创建 Prism Bootstrapper**（App.Wpf/Bootstrapper/AppBootstrapper.cs），配置 DI 容器注册所有算子和适配器

5. **创建 WPF Shell 窗口**（App.Wpf/Views/ShellWindow.xaml），包含：
   - 左侧：TreeView 步骤列表
   - 中间：图像显示区域
   - 右侧：属性面板

6. **验证端到端**：加载流程 JSON → 点执行 → 采集 → 灰度化 → Blob → 显示结果

## 重要提醒

- 所有算子只操作 ProcessContext 的抽象接口，不直接使用 Halcon 类型
- 接口已冻结，只增不减
- Halcon 调用只能通过 HalconEngine 进行
- 代码风格遵循 .editorconfig
- 每个 .cs 文件使用 file-scoped namespace

## 参考文档

请先阅读以下文档了解完整上下文：
- 软件方案设计书.md
- 软件架构可视化文档.md
- docs/templates/模板使用说明.md

## 工作目录

e:\WXAPP\VisionInspect\