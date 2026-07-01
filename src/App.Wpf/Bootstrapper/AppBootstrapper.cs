using System;
using System.Collections.Generic;
using SystemLib.Core;
using SystemLib.Services;
using CommLib.Core;
using CommLib.Modbus;
using CommLib.SerialPort;
using CommLib.TcpUdp;
using CommLib.WebApi;
using ImageLib.Core;
using ImageLib.HalconBridge;
using ImageLib.Operators;
using VisionInspect.Services;
using WorkflowEngine.Core;
using Prism.Ioc;

namespace VisionInspect.Bootstrapper
{
    public static class AppBootstrapper
    {
        public static void Register(IContainerRegistry containerRegistry)
        {
            // Halcon 引擎
            HalconEngine.Instance.Initialize();

            // 图像处理器（算子）
            containerRegistry.RegisterSingleton<IImageProcessor, AcquisitionProcessor>(nameof(AcquisitionProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, GrayScaleProcessor>(nameof(GrayScaleProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, BlobAnalysisProcessor>(nameof(BlobAnalysisProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, MeanFilterProcessor>(nameof(MeanFilterProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, MedianFilterProcessor>(nameof(MedianFilterProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, GaussianFilterProcessor>(nameof(GaussianFilterProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, ImageEnhancementProcessor>(nameof(ImageEnhancementProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, ShapeBasedMatchingProcessor>(nameof(ShapeBasedMatchingProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, AffineTransformProcessor>(nameof(AffineTransformProcessor));
            containerRegistry.RegisterSingleton<IImageProcessor, CaliperMeasurementProcessor>(nameof(CaliperMeasurementProcessor));

            // 通讯适配器
            containerRegistry.RegisterSingleton<ICommunicationAdapter, SerialPortAdapter>("SerialPort");
            containerRegistry.RegisterSingleton<ICommunicationAdapter, ModbusTcpAdapter>("ModbusTcp");
            containerRegistry.RegisterSingleton<ICommunicationAdapter, TcpClientAdapter>("TcpClient");
            containerRegistry.RegisterSingleton<ICommunicationAdapter, TcpServerAdapter>("TcpServer");
            containerRegistry.RegisterSingleton<ICommunicationAdapter, WebApiAdapter>("WebApi");

            // 流程引擎
            containerRegistry.RegisterSingleton<IRecipeExecutor, RecipeExecutor>();

            // 系统服务
            containerRegistry.RegisterSingleton<ILogger, LogService>();
            containerRegistry.RegisterSingleton<IFileOperator, FileService>();
            containerRegistry.RegisterSingleton<IConfigManager, ConfigManager>();
            containerRegistry.RegisterSingleton<IAlarmManager, AlarmService>();
            containerRegistry.RegisterSingleton<IDatabaseOperator, DatabaseService>();

            // 视图
            containerRegistry.RegisterForNavigation<Views.ShellWindow>();
        }
    }
}
