using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageLib.Core;
using ImageLib.HalconBridge;
using ImageLib.Operators;
using SystemLib.Core;
using WorkflowEngine.Core;
using VisionInspect.Services;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Win32;

namespace VisionInspect.ViewModels
{
    public class ShellWindowViewModel : BindableBase
    {
        private readonly IRecipeExecutor _recipeExecutor;
        private readonly ILogger _logger;
        private InspectionRecipe? _currentRecipe;
        private CancellationTokenSource? _executionCts;

        private ObservableCollection<StepNodeViewModel> _steps = new();
        private StepNodeViewModel? _selectedStepNode;
        private ObservableCollection<ResultRowViewModel> _results = new();
        private ImageSource? _currentImage;
        private string _statusText = "就绪";
        private bool _isExecuting;
        private string _progressText = "";
        private int _currentStepIndex = -1;

        // 属性面板绑定
        private string _selectedStepName = "";
        private string _selectedProcessorType = "";
        private bool _selectedStepStopOnFailure = true;
        private ObservableCollection<ParamViewModel> _selectedStepParams = new();

        // 设备状态
        private string _cameraStatus = "未连接";
        private string _plcStatus = "未连接";

        public ShellWindowViewModel(IRecipeExecutor recipeExecutor, ILogger logger)
        {
            _recipeExecutor = recipeExecutor;
            _logger = logger;

            NewRecipeCommand = new DelegateCommand(ExecuteNewRecipe);
            LoadRecipeCommand = new DelegateCommand(ExecuteLoadRecipe);
            SaveRecipeCommand = new DelegateCommand(ExecuteSaveRecipe);
            AddStepCommand = new DelegateCommand(ExecuteAddStep);
            DeleteStepCommand = new DelegateCommand(ExecuteDeleteStep, () => SelectedStepNode != null);
            MoveUpCommand = new DelegateCommand(ExecuteMoveUp, () => SelectedStepNode != null);
            MoveDownCommand = new DelegateCommand(ExecuteMoveDown, () => SelectedStepNode != null);
            RunCommand = new DelegateCommand(async () => await ExecuteRunAll(), () => !IsExecuting);
            StepRunCommand = new DelegateCommand(async () => await ExecuteStepRun(), () => !IsExecuting);
            StopCommand = new DelegateCommand(ExecuteStop, () => IsExecuting);
            ClearLogCommand = new DelegateCommand(ExecuteClearLog);

            LoadAvailableProcessors();
            _logger.Info("视觉检测平台已启动");
        }

        // ========== 步骤列表 ==========

        public ObservableCollection<StepNodeViewModel> Steps
        {
            get => _steps;
            set => SetProperty(ref _steps, value);
        }

        public StepNodeViewModel? SelectedStepNode
        {
            get => _selectedStepNode;
            set
            {
                if (SetProperty(ref _selectedStepNode, value))
                {
                    RefreshPropertyPanel();
                    ((DelegateCommand)DeleteStepCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)MoveUpCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)MoveDownCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // ========== 结果列表 ==========

        public ObservableCollection<ResultRowViewModel> Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
        }

        // ========== 图像显示 ==========

        public ImageSource? CurrentImage
        {
            get => _currentImage;
            set
            {
                if (SetProperty(ref _currentImage, value))
                {
                    RaisePropertyChanged(nameof(HasImage));
                }
            }
        }

        public bool HasImage => CurrentImage != null;

        // ========== 执行状态 ==========

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    ((DelegateCommand)RunCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)StepRunCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)StopCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // ========== 设备状态 ==========

        public string CameraStatus
        {
            get => _cameraStatus;
            set => SetProperty(ref _cameraStatus, value);
        }

        public string PlcStatus
        {
            get => _plcStatus;
            set => SetProperty(ref _plcStatus, value);
        }

        // ========== 属性面板 ==========

        public string SelectedStepName
        {
            get => _selectedStepName;
            set
            {
                if (SetProperty(ref _selectedStepName, value) && _selectedStepNode != null)
                {
                    _selectedStepNode.StepName = value;
                    _selectedStepNode.Step!.Name = value;
                }
            }
        }

        public string SelectedProcessorType
        {
            get => _selectedProcessorType;
            set
            {
                if (SetProperty(ref _selectedProcessorType, value) && _selectedStepNode != null)
                {
                    _selectedStepNode.StepType = value;
                    _selectedStepNode.Step!.SubSteps[0].ProcessorType = value;
                }
            }
        }

        public bool SelectedStepStopOnFailure
        {
            get => _selectedStepStopOnFailure;
            set
            {
                if (SetProperty(ref _selectedStepStopOnFailure, value) && _selectedStepNode != null)
                {
                    _selectedStepNode.Step!.StopOnFailure = value;
                }
            }
        }

        public ObservableCollection<ParamViewModel> SelectedStepParams
        {
            get => _selectedStepParams;
            set => SetProperty(ref _selectedStepParams, value);
        }

        public ObservableCollection<string> AvailableProcessors { get; } = new();

        // ========== 日志 ==========

        public ObservableCollection<LogEntryViewModel> LogEntries =>
            (_logger is LogService logService) ? logService.Entries : new ObservableCollection<LogEntryViewModel>();

        // ========== 命令 ==========

        public ICommand NewRecipeCommand { get; }
        public ICommand LoadRecipeCommand { get; }
        public ICommand SaveRecipeCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand StepRunCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearLogCommand { get; }

        // ========== 命令实现 ==========

        private void ExecuteNewRecipe()
        {
            _currentRecipe = new InspectionRecipe
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Name = "新建流程",
                Steps = new List<InspectionStep>()
            };
            _currentStepIndex = -1;
            Steps.Clear();
            Results.Clear();
            CurrentImage = null;
            StatusText = "已创建新流程";
            _logger.Info("创建新流程");
        }

        private void ExecuteLoadRecipe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "流程文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "加载检测流程"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var recipe = RecipeSerializer.Deserialize(json);
                    if (recipe != null)
                    {
                        _currentRecipe = recipe;
                        _currentStepIndex = -1;
                        RefreshSteps();
                        Results.Clear();
                        CurrentImage = null;
                        StatusText = $"已加载: {recipe.Name} ({recipe.Steps.Count} 步骤)";
                        _logger.Info($"加载流程: {recipe.Name} ({recipe.Steps.Count} 步骤)");
                    }
                    else
                    {
                        StatusText = "加载失败: 无法解析流程文件";
                        _logger.Error("加载流程失败: JSON 解析错误");
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"加载失败: {ex.Message}";
                    _logger.Error($"加载流程异常: {ex.Message}", ex);
                }
            }
        }

        private void ExecuteSaveRecipe()
        {
            if (_currentRecipe == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "流程文件 (*.json)|*.json",
                Title = "保存检测流程",
                FileName = $"{_currentRecipe.Name}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = RecipeSerializer.Serialize(_currentRecipe);
                    File.WriteAllText(dialog.FileName, json);
                    StatusText = $"已保存: {dialog.FileName}";
                    _logger.Info($"保存流程: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    StatusText = $"保存失败: {ex.Message}";
                    _logger.Error($"保存流程异常: {ex.Message}", ex);
                }
            }
        }

        private void ExecuteAddStep()
        {
            if (_currentRecipe == null)
            {
                _currentRecipe = new InspectionRecipe
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = "新建流程",
                    Steps = new List<InspectionStep>()
                };
            }

            var stepIndex = _currentRecipe.Steps.Count + 1;
            var step = new InspectionStep
            {
                Id = $"step_{stepIndex}",
                Name = $"步骤{stepIndex}",
                Type = StepType.Inspection,
                SubSteps = new List<ProcessorNode>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        ProcessorType = "Acquisition",
                        Parameters = new Dictionary<string, object>
                        {
                            ["ImagePath"] = ""
                        }
                    }
                }
            };

            _currentRecipe.Steps.Add(step);
            RefreshSteps();
            StatusText = $"已添加步骤: {step.Name}";
            _logger.Info($"添加步骤: {step.Name} [{step.SubSteps[0].ProcessorType}]");
        }

        private void ExecuteDeleteStep()
        {
            if (_currentRecipe == null || _selectedStepNode == null) return;
            var idx = Steps.IndexOf(_selectedStepNode);
            if (idx < 0) return;

            var name = _selectedStepNode.StepName;
            _currentRecipe.Steps.RemoveAt(idx);
            RefreshSteps();
            SelectedStepNode = null;
            StatusText = $"已删除步骤: {name}";
            _logger.Info($"删除步骤: {name}");
        }

        private void ExecuteMoveUp()
        {
            if (_currentRecipe == null || _selectedStepNode == null) return;
            var idx = Steps.IndexOf(_selectedStepNode);
            if (idx <= 0) return;

            (_currentRecipe.Steps[idx], _currentRecipe.Steps[idx - 1]) =
                (_currentRecipe.Steps[idx - 1], _currentRecipe.Steps[idx]);
            RefreshSteps();
            SelectedStepNode = Steps[idx - 1];
            _logger.Debug($"步骤上移: {_selectedStepNode.StepName}");
        }

        private void ExecuteMoveDown()
        {
            if (_currentRecipe == null || _selectedStepNode == null) return;
            var idx = Steps.IndexOf(_selectedStepNode);
            if (idx < 0 || idx >= Steps.Count - 1) return;

            (_currentRecipe.Steps[idx], _currentRecipe.Steps[idx + 1]) =
                (_currentRecipe.Steps[idx + 1], _currentRecipe.Steps[idx]);
            RefreshSteps();
            SelectedStepNode = Steps[idx + 1];
            _logger.Debug($"步骤下移: {_selectedStepNode.StepName}");
        }

        private async Task ExecuteRunAll()
        {
            if (_currentRecipe == null)
            {
                StatusText = "请先加载或新建流程";
                _logger.Warning("执行失败: 无可用流程");
                return;
            }

            IsExecuting = true;
            Results.Clear();
            _currentStepIndex = -1;
            _executionCts = new CancellationTokenSource();

            _logger.Info($"开始执行全部步骤: {_currentRecipe.Name} (共 {_currentRecipe.Steps.Count} 步)");

            try
            {
                // 重置所有步骤状态
                foreach (var s in Steps) s.StatusText = "待执行";
                ProgressText = $"0/{_currentRecipe.Steps.Count}";

                var result = await _recipeExecutor.ExecuteAsync(_currentRecipe, _executionCts.Token);

                int stepIdx = 0;
                foreach (var kv in result.StepResults)
                {
                    stepIdx++;
                    var stepNode = Steps.FirstOrDefault(s => s.Step!.Id == kv.Key);
                    var stepResult = kv.Value as StepResult;
                    if (stepResult == null) continue;
                    Results.Add(new ResultRowViewModel
                    {
                        StepName = stepNode?.StepName ?? stepResult.StepName,
                        Result = stepResult.IsSuccess ? "OK" : "NG",
                        ElapsedMs = (int)stepResult.ElapsedMs,
                        OutputSummary = stepResult.IsSuccess
                            ? $"输出 {stepResult.Outputs.Count} 项"
                            : stepResult.ErrorMessage ?? "未知错误"
                    });

                    if (stepNode != null)
                    {
                        stepNode.StatusText = stepResult.IsSuccess ? "✓ 完成" : "✗ 失败";
                        if (stepResult.IsSuccess)
                            _logger.Info($"步骤 [{stepNode.StepName}] 完成, 耗时 {stepResult.ElapsedMs:F1}ms");
                        else
                            _logger.Warning($"步骤 [{stepNode.StepName}] 失败: {stepResult.ErrorMessage}");
                    }

                    ProgressText = $"{stepIdx}/{_currentRecipe.Steps.Count}";
                }

                var successCount = result.StepResults.Count - result.FailedStepIds.Count;
                StatusText = result.IsSuccess ? "全部执行完成" : $"执行完成: {successCount}/{result.StepResults.Count} 成功";
                _logger.Info($"执行完成: {successCount}/{result.StepResults.Count} 成功, 总耗时 {result.Elapsed.TotalSeconds:F1}s");
            }
            catch (OperationCanceledException)
            {
                StatusText = "执行已停止";
                _logger.Warning("执行被用户取消");
            }
            catch (Exception ex)
            {
                StatusText = $"执行异常: {ex.Message}";
                _logger.Error($"执行异常: {ex.Message}", ex);
            }
            finally
            {
                IsExecuting = false;
                _executionCts = null;
            }
        }

        private async Task ExecuteStepRun()
        {
            if (_currentRecipe == null || _currentRecipe.Steps.Count == 0)
            {
                StatusText = "请先添加步骤";
                _logger.Warning("单步执行失败: 无可用步骤");
                return;
            }

            IsExecuting = true;
            _executionCts = new CancellationTokenSource();

            try
            {
                _currentStepIndex++;
                if (_currentStepIndex >= _currentRecipe.Steps.Count)
                    _currentStepIndex = 0;

                var step = _currentRecipe.Steps[_currentStepIndex];
                var partialRecipe = new InspectionRecipe
                {
                    Id = _currentRecipe.Id,
                    Name = _currentRecipe.Name,
                    Steps = new List<InspectionStep> { step }
                };

                ProgressText = $"单步: {_currentStepIndex + 1}/{_currentRecipe.Steps.Count}";
                StatusText = $"正在执行: {step.Name}...";
                _logger.Info($"单步执行 [{_currentStepIndex + 1}/{_currentRecipe.Steps.Count}]: {step.Name}");

                var result = await _recipeExecutor.ExecuteAsync(partialRecipe, _executionCts.Token);
                var stepResult = result.StepResults.Values.FirstOrDefault() as StepResult;

                if (stepResult != null)
                {
                    Results.Insert(0, new ResultRowViewModel
                    {
                        StepName = step.Name,
                        Result = stepResult.IsSuccess ? "OK" : "NG",
                        ElapsedMs = (int)stepResult.ElapsedMs,
                        OutputSummary = stepResult.IsSuccess ? $"输出 {stepResult.Outputs.Count} 项" : stepResult.ErrorMessage ?? ""
                    });

                    if (Steps.Count > _currentStepIndex)
                        Steps[_currentStepIndex].StatusText = stepResult.IsSuccess ? "✓ 完成" : "✗ 失败";

                    if (stepResult.IsSuccess)
                        _logger.Info($"单步 [{step.Name}] 完成, 耗时 {stepResult.ElapsedMs:F1}ms");
                    else
                        _logger.Warning($"单步 [{step.Name}] 失败: {stepResult.ErrorMessage}");

                    // 尝试显示图像
                    foreach (var kv in stepResult.Outputs)
                    {
                        if (kv.Value is IImageData imgData)
                        {
                            DisplayImage(imgData);
                            break;
                        }
                    }
                }

                StatusText = $"单步完成: {step.Name}";
            }
            catch (OperationCanceledException)
            {
                StatusText = "已停止";
                _logger.Warning("单步执行被取消");
            }
            catch (Exception ex)
            {
                StatusText = $"单步异常: {ex.Message}";
                _logger.Error($"单步执行异常: {ex.Message}", ex);
            }
            finally
            {
                IsExecuting = false;
                _executionCts = null;
            }
        }

        private void ExecuteStop()
        {
            _executionCts?.Cancel();
            _logger.Info("用户请求停止执行");
        }

        private void ExecuteClearLog()
        {
            if (_logger is LogService logService)
                logService.Entries.Clear();
            _logger.Debug("日志已清空");
        }

        // ========== 辅助方法 ==========

        private void RefreshSteps()
        {
            Steps.Clear();
            if (_currentRecipe == null) return;

            foreach (var step in _currentRecipe.Steps)
            {
                var processorType = step.SubSteps?.FirstOrDefault()?.ProcessorType ?? "Unknown";
                Steps.Add(new StepNodeViewModel
                {
                    Step = step,
                    StepName = step.Name,
                    StepType = processorType,
                    StatusText = "待执行"
                });
            }
        }

        private void RefreshPropertyPanel()
        {
            if (_selectedStepNode?.Step == null)
            {
                _selectedStepName = "";
                _selectedProcessorType = "";
                _selectedStepStopOnFailure = true;
                SelectedStepParams.Clear();
                RaisePropertyChanged(nameof(SelectedStepName));
                RaisePropertyChanged(nameof(SelectedProcessorType));
                RaisePropertyChanged(nameof(SelectedStepStopOnFailure));
                return;
            }

            var step = _selectedStepNode.Step;
            _selectedStepName = step.Name;
            _selectedProcessorType = step.SubSteps?.FirstOrDefault()?.ProcessorType ?? "";
            _selectedStepStopOnFailure = step.StopOnFailure;
            RaisePropertyChanged(nameof(SelectedStepName));
            RaisePropertyChanged(nameof(SelectedProcessorType));
            RaisePropertyChanged(nameof(SelectedStepStopOnFailure));

            SelectedStepParams.Clear();
            var node = step.SubSteps?.FirstOrDefault();
            if (node?.Parameters != null)
            {
                foreach (var kv in node.Parameters)
                {
                    var paramVm = new ParamViewModel
                    {
                        Name = kv.Key,
                        Value = kv.Value?.ToString() ?? "",
                        ValueType = GetParamType(kv.Value)
                    };
                    paramVm.ValueChanged += (s, val) =>
                    {
                        node.Parameters[kv.Key] = ConvertParamValue(val, kv.Value);
                    };
                    SelectedStepParams.Add(paramVm);
                }
            }
        }

        private void LoadAvailableProcessors()
        {
            AvailableProcessors.Clear();

            var processorIds = typeof(AcquisitionProcessor).Assembly
                .GetTypes()
                .Where(type => typeof(IImageProcessor).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => Activator.CreateInstance(type) as IImageProcessor)
                .Where(processor => processor != null)
                .Select(processor => processor!.Id)
                .Distinct()
                .OrderBy(id => id);

            foreach (var processorId in processorIds)
            {
                AvailableProcessors.Add(processorId);
            }
        }

        private static string GetParamType(object? value)
        {
            if (value is bool) return "bool";
            if (value is int or long) return "int";
            if (value is double or float) return "double";
            return "string";
        }

        private static object ConvertParamValue(string newValue, object? original)
        {
            if (original is bool) return bool.TryParse(newValue, out var b) && b;
            if (original is int) return int.TryParse(newValue, out var i) ? i : 0;
            if (original is long) return long.TryParse(newValue, out var l) ? l : 0L;
            if (original is double) return double.TryParse(newValue, out var d) ? d : 0.0;
            if (original is float) return float.TryParse(newValue, out var f) ? f : 0f;
            return newValue;
        }

        private void DisplayImage(IImageData imageData)
        {
            try
            {
                var engine = HalconEngine.Instance;
                var tempPath = Path.Combine(Path.GetTempPath(), $"vis_temp_{Guid.NewGuid():N}.bmp");
                engine.WriteImage(imageData, tempPath);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(tempPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                CurrentImage = bitmap;

                try { File.Delete(tempPath); } catch { }
                _logger.Debug("图像已更新");
            }
            catch
            {
                // Halcon 不可用时忽略
            }
        }
    }

    // ========== 子 ViewModel ==========

    public class StepNodeViewModel : BindableBase
    {
        private string _stepName = "";
        private string _stepType = "";
        private string _statusText = "待执行";
        private InspectionStep? _step;

        public string StepName { get => _stepName; set => SetProperty(ref _stepName, value); }
        public string StepType { get => _stepType; set => SetProperty(ref _stepType, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public InspectionStep? Step { get => _step; set => SetProperty(ref _step, value); }
    }

    public class ResultRowViewModel : BindableBase
    {
        private string _stepName = "";
        private string _result = "";
        private int _elapsedMs;
        private string _outputSummary = "";

        public string StepName { get => _stepName; set => SetProperty(ref _stepName, value); }
        public string Result { get => _result; set => SetProperty(ref _result, value); }
        public int ElapsedMs { get => _elapsedMs; set => SetProperty(ref _elapsedMs, value); }
        public string OutputSummary { get => _outputSummary; set => SetProperty(ref _outputSummary, value); }
    }

    public class ParamViewModel : BindableBase
    {
        private string _name = "";
        private string _value = "";
        private string _valueType = "string";

        public event EventHandler<string>? ValueChanged;

        public string Name { get => _name; set => SetProperty(ref _name, value); }

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    ValueChanged?.Invoke(this, value);
                }
            }
        }

        public string ValueType { get => _valueType; set => SetProperty(ref _valueType, value); }
    }
}
