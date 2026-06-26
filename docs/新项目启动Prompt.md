# 开发接续 Prompt

将此文件内容全部复制到新 AI 对话窗口，即可接续开发。

---

## 项目背景

我在开发一个**通用视觉检测平台（VisionInspect）**，面向半导体芯片检测行业。

**技术栈**：WPF + .NET 6.0/8.0 + Prism 8.x (DryIoc) + Halcon 22.x + MaterialDesign + HandyControl + NLog + SQLite

**GitHub 仓库**：https://github.com/ioioppk/PrismAOI

---

## 当前阶段

**第二阶段：能用（第 5-10 周）** → 第 5 周已完成，当前准备开始第 6 周

---

## 整体进度

| 阶段 | 名称 | 状态 |
|------|------|------|
| 第一阶段 | 地基（第 1-4 周） | ✅ 已完成 |
| 第二阶段 | 能用（第 5-10 周） | 🔄 进行中 |
| 第三阶段 | 稳定（第 11-16 周） | ⬜ 待开始 |
| 第四阶段 | 扩展（第 17 周起） | ⬜ 待开始 |

---

## 第一阶段已完成（✅ v0.1.0）

- [x] 15 个源码项目 + 4 个测试项目，sln 完整
- [x] 全部核心接口定义并冻结
- [x] ImageLib（Core + HalconBridge + Operators，含 3 个算子：采集/灰度化/Blob）
- [x] CommLib（Core + SerialPort + Modbus + TcpUdp + WebApi + PLC.Mitsubishi + PLC.Siemens + SecsGem）
- [x] SystemLib（Core 接口 + Services 实现：日志/文件/配置/数据库/报警）
- [x] WorkflowEngine（RecipeExecutor + RecipeSerializer + ProcessorDiscovery 自动发现）
- [x] App.Wpf 主程序（Prism Bootstrapper + Shell 三栏布局 + MVVM）
- [x] 64 个单元测试，100% 通过
- [x] 端到端验证通过（三步骤流程串联）
- [x] 5 项技术决策落地（Halcon 隔离、Modbus 原生实现、TreeView 编辑器、对象池、枚举序列化）
- [x] .editorconfig 代码规范
- [x] 阶段文档齐全

---

## 第二阶段第 5 周已完成（✅）

流程编辑器完善：
- [x] 新增/删除步骤（NewRecipeCommand, AddStepCommand, DeleteStepCommand）
- [x] 上移/下移步骤（MoveStepUpCommand, MoveStepDownCommand）
- [x] 单步执行（ExecuteStepCommand）
- [x] 子步骤编辑（添加/删除/上移/下移算子节点）
- [x] 参数编辑控件（数值/字符串/布尔/枚举，根据 ParamType 动态显示）
- [x] 算子自动发现与下拉选择
- [x] 工具栏按钮 7 个（新建/加载/保存/+/-/▲/▼/执行/单步）
- [x] 新增 ParamTypeToVisibilityConverter 转换器
- [x] 新增 ParameterEditViewModel

关键代码位置：
- `src/App.Wpf/ViewModels/ShellWindowViewModel.cs` — 全部编辑器命令
- `src/App.Wpf/Views/ShellWindow.xaml` — TreeView + 工具栏 + 属性面板
- `src/App.Wpf/Converters/` — ParamTypeToVisibilityConverter, InverseBooleanToVisibilityConverter

---

## 第二阶段第 6 周（⬜ 待开始）：算子扩充至 10 个

需新增 7 个算子（现有 3 个 → 目标 10 个）：
1. 均值滤波（MeanFilterProcessor）
2. 中值滤波（MedianFilterProcessor）
3. 高斯滤波（GaussianFilterProcessor）
4. 图像增强（ImageEnhancementProcessor）
5. 模板匹配（ShapeBasedMatchingProcessor）
6. 仿射变换（AffineTransformProcessor）
7. 卡尺测量（CaliperMeasurementProcessor）

替代方案（如 Halcon 未安装可先做纯逻辑版本）：
- 阈值判定（ThresholdJudgementProcessor）
- 极坐标变换（PolarTransformProcessor）

---

## 第二阶段后续计划（第 7-10 周）

| 周次 | 主题 | 内容 |
|------|------|------|
| 第 7 周 | 通讯协议扩充 | TCP Client/Server 适配器、WebAPI 适配器、三菱/西门子 PLC、流程中接入通讯节点 |
| 第 8 周 | 设备管理 | Basler 相机 SDK 封装、实时采集、光源控制器、设备管理面板 |
| 第 9 周 | 运行监控 | 实时检测画面、OK/NG 统计面板、NG 图像自动保存、报警联动 |
| 第 10 周 | 集成测试 + 打磨 | 全模块串联、异常处理补全、日志完善、内存泄漏排查 |

---

## 基础设施（✅）

- [x] Git 仓库已初始化，推送到 GitHub：`ioioppk/PrismAOI`
- [x] `.gitignore` 已配置（排除 bin/obj/构建产物）
- [x] `README.md` 已补写（架构图、技术栈、项目结构、构建说明、文档索引）
- [x] 提交规范已建立（Conventional Commits：feat/fix/refactor/test/docs/chore）
- [x] `docs/` 目录结构已清理（归档根目录中文文档）
- [x] CI/CD 流水线已搭建（GitHub Actions：每次推送自动 build + test）

---

## 编译 & 测试命令

```bash
# 还原依赖
cd e:\WXAPP\VisionInspect
dotnet restore VisionInspect.sln

# 构建
dotnet build VisionInspect.sln -c Debug

# 运行全部测试（目前 64 项，100% 通过）
dotnet test VisionInspect.sln

# Git（git.exe 路径）
C:\Program Files\Git\cmd\git.exe
```

当前状态：**编译 0 错误、64 测试全部通过**

---

## 技术债务（6 项）

| 编号 | 内容 | 严重程度 | 优先级 | 计划版本 |
|------|------|---------|--------|---------|
| DEBT-001 | Dictionary 反序列化为 JsonElement | 低 | P3 | V0.2.0 |
| DEBT-002 | Moq 4.20.0 安全漏洞 | 低 | P3 | V0.2.0 |
| DEBT-003 | 缺少代码覆盖率工具 | 中 | P2 | V0.2.0 |
| DEBT-004 | 无 CI/CD 流水线 | 中 | P2 | ✅ 已偿还 |
| DEBT-005 | Halcon 图像处理未做真实环境测试 | 中 | P2 | V0.2.0 |
| DEBT-006 | 通讯协议未做真实设备联调 | 中 | P2 | V0.2.0 |

---

## 核心架构约束

1. **Halcon 隔离策略**：所有算子只操作 ProcessContext 抽象接口，不直接使用 Halcon 类型（ImageData/RegionData 封装隔离）
2. **接口冻结**：已定义的接口只增不减
3. **Halcon 调用入口**：只能通过 HalconEngine 单例
4. **编译开关**：`HAS_HALCON` 条件编译，未安装 Halcon 的环境走降级分支
5. **file-scoped namespace**：所有 `.cs` 文件使用
6. **Commit 规范**：`feat:` / `fix:` / `refactor:` / `test:` / `docs:` / `chore:`
7. **提交前验证**：`dotnet build` + `dotnet test` 零错误

---

## 项目结构

```
VisionInspect/
├── .editorconfig
├── .gitignore
├── README.md
├── VisionInspect.sln
├── demo_recipe.json
├── .github/workflows/dotnet.yml   ← CI/CD
├── src/
│   ├── ImageLib.Core/             图像处理核心接口
│   ├── ImageLib.HalconBridge/     Halcon 引擎封装
│   ├── ImageLib.Operators/        3 个基础算子
│   ├── CommLib.Core/              通讯核心接口
│   ├── CommLib.SerialPort/        串口适配器
│   ├── CommLib.Modbus/            Modbus TCP
│   ├── CommLib.TcpUdp/            TCP/UDP
│   ├── CommLib.WebApi/            WebAPI
│   ├── CommLib.PLC.Mitsubishi/    三菱 PLC
│   ├── CommLib.PLC.Siemens/       西门子 PLC
│   ├── CommLib.SecsGem/           SECS/GEM
│   ├── SystemLib.Core/            系统接口
│   ├── SystemLib.Services/        系统服务实现
│   ├── WorkflowEngine.Core/       流程引擎
│   └── App.Wpf/                   WPF 主程序
│       ├── Bootstrapper/
│       ├── Converters/
│       ├── ViewModels/
│       └── Views/
├── tests/
│   ├── ImageLib.Tests/            22 项
│   ├── CommLib.Tests/             8 项
│   ├── SystemLib.Tests/           19 项
│   └── WorkflowEngine.Tests/      15 项
└── docs/
    ├── 软件方案设计书.md
    ├── 软件架构可视化文档.md
    ├── 开发进度记录.md
    ├── 提交规范.md
    ├── recipe-schema.json
    ├── stages/
    │   ├── 01-项目启动/
    │   ├── 02-阶段一-地基/
    │   ├── 03-阶段二-能用/
    │   ├── 04-阶段三-稳定/
    │   └── 05-阶段四-扩展/
    └── templates/
```

---

## 下一步任务（请优先完成）

1. **算子扩充至 10 个（第 6 周）**：在 `src/ImageLib.Operators/` 下新增 7 个算子文件
   - 优先实现：均值滤波、中值滤波、高斯滤波、图像增强、模板匹配、仿射变换、卡尺测量
   - 每个算子遵循 `IImageProcessor` 接口，使用 `[Processor]` 属性标记
   - 如 Halcon 未安装，先用 `#if HAS_HALCON` / `#else` 搭骨架，`ExecuteAsync` 返回模拟数据
2. **偿还 DEBT-001**：实现 Dictionary<string, object> 的自定义 JsonConverter，修复反序列化后 JsonElement 问题
3. **补充算子单元测试**：新增的 7 个算子各至少补充 1 个基础测试

---

## 参考文档索引

| 文档 | 路径 |
|------|------|
| 软件方案设计书 | [docs/软件方案设计书.md](docs/软件方案设计书.md) |
| 架构可视化文档 | [docs/软件架构可视化文档.md](docs/软件架构可视化文档.md) |
| 第一阶段总结 | [docs/stages/02-阶段一-地基/阶段总结.md](docs/stages/02-阶段一-地基/阶段总结.md) |
| 测试报告 | [docs/stages/02-阶段一-地基/测试报告.md](docs/stages/02-阶段一-地基/测试报告.md) |
| 技术债务记录 | [docs/stages/02-阶段一-地基/技术债务记录.md](docs/stages/02-阶段一-地基/技术债务记录.md) |
| 问题跟踪表 | [docs/stages/02-阶段一-地基/问题跟踪表.md](docs/stages/02-阶段一-地基/问题跟踪表.md) |
| 开发进度记录 | [docs/开发进度记录.md](docs/开发进度记录.md) |
| 提交规范 | [docs/提交规范.md](docs/提交规范.md) |
| README | [README.md](README.md) |
