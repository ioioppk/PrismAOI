using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ImageLib.Core;
using ImageLib.HalconBridge;

namespace ImageLib.Operators
{
    [Processor("灰度化", "Preprocessing", "将彩色图像转换为灰度图像")]
    public sealed class GrayScaleProcessor : IImageProcessor
    {
        public string Id => "GrayScale";
        public string Name => "灰度化";
        public string Category => "Preprocessing";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("InputImage", ParamType.Image, null, "输入图像")
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "输出的灰度图像")
        };

        public async Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var inputImage = context.InputImage;
                if (inputImage == null && context.Images.TryGetValue("Input", out var img))
                {
                    inputImage = img;
                }

                if (inputImage == null)
                {
                    return ProcessResult.Failure("No input image found. Set context.InputImage or context.Images[\"Input\"].");
                }

                var grayImage = await Task.Run(() => HalconEngine.Instance.Rgb1ToGray(inputImage), ct);

                context.OutputImage = grayImage;
                context.Images["Output"] = grayImage;

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