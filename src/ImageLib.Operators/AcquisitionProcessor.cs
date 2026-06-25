using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ImageLib.Core;
using ImageLib.HalconBridge;

namespace ImageLib.Operators
{
    [Processor("图像采集", "Acquisition", "从文件或相机采集图像")]
    public sealed class AcquisitionProcessor : IImageProcessor
    {
        public string Id => "Acquisition";
        public string Name => "图像采集";
        public string Category => "Acquisition";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("ImagePath", ParamType.String, "", "图像文件路径"),
            new ParamDef("CameraId", ParamType.String, "", "相机ID（留空则从文件读取）")
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "输出的图像数据")
        };

        public async Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var imagePath = context.Get<string>("ImagePath") ?? string.Empty;
                var cameraId = context.Get<string>("CameraId") ?? string.Empty;

                IImageData image;

                if (!string.IsNullOrEmpty(cameraId))
                {
                    return ProcessResult.Failure("Camera acquisition is not yet implemented.");
                }

                if (string.IsNullOrEmpty(imagePath))
                {
                    return ProcessResult.Failure("ImagePath parameter is required.");
                }

                image = await Task.Run(() => HalconEngine.Instance.ReadImage(imagePath), ct);

                context.OutputImage = image;
                context.InputImage = image;
                context.Images["Output"] = image;

                sw.Stop();
                var result = ProcessResult.Success();
                result.Elapsed = sw.Elapsed;
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var result = ProcessResult.Failure(ex.Message);
                result.Elapsed = sw.Elapsed;
                return result;
            }
        }
    }
}