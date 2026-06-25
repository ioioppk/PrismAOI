using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ImageLib.Core;
using ImageLib.HalconBridge;

namespace ImageLib.Operators
{
    [Processor("Blob分析", "Inspection", "阈值分割 + 连通域分析 + 形状筛选")]
    public sealed class BlobAnalysisProcessor : IImageProcessor
    {
        public string Id => "BlobAnalysis";
        public string Name => "Blob分析";
        public string Category => "Inspection";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("MinArea", ParamType.Double, 100.0, "最小面积", 0.0, null),
            new ParamDef("MaxArea", ParamType.Double, 999999.0, "最大面积", 0.0, null),
            new ParamDef("Threshold", ParamType.Double, 128.0, "灰度阈值", 0.0, 255.0),
            new ParamDef("Invert", ParamType.Bool, false, "是否反选区域")
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("BlobCount", ParamType.Int, null, "检测到的Blob数量"),
            new ParamDef("OutputRegion", ParamType.Region, null, "输出的区域数据")
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

                double minArea = 100.0;
                double maxArea = 999999.0;
                double threshold = 128.0;
                bool invert = false;

                if (context["MinArea"] is double ma) minArea = ma;
                if (context["MaxArea"] is double mxa) maxArea = mxa;
                if (context["Threshold"] is double th) threshold = th;
                if (context["Invert"] is bool inv) invert = inv;

                double minGray = invert ? threshold : 0.0;
                double maxGray = invert ? 255.0 : threshold;

                IRegionData thresholdRegion = null;
                IRegionData connectedRegion = null;
                IRegionData selectedRegion = null;

                try
                {
                    thresholdRegion = await Task.Run(() =>
                        HalconEngine.Instance.Threshold(inputImage, minGray, maxGray), ct);

                    connectedRegion = await Task.Run(() =>
                        HalconEngine.Instance.Connection(thresholdRegion), ct);

                    selectedRegion = await Task.Run(() =>
                        HalconEngine.Instance.SelectShape(connectedRegion, "area", "and", minArea, maxArea), ct);

                    int blobCount = HalconEngine.Instance.CountObj(selectedRegion);

                    context.OutputRegion = selectedRegion;
                    context.Regions["Output"] = selectedRegion;
                    context["BlobCount"] = blobCount;

                    var outputValues = new Dictionary<string, object>
                    {
                        { "BlobCount", blobCount },
                        { "OutputRegion", selectedRegion }
                    };

                    sw.Stop();
                    var result = ProcessResult.Success(new Dictionary<string, object>(outputValues));
                    result.Elapsed = sw.Elapsed;
                    return result;
                }
                catch
                {
                    thresholdRegion?.Dispose();
                    connectedRegion?.Dispose();
                    selectedRegion?.Dispose();
                    throw;
                }
                finally
                {
                    thresholdRegion?.Dispose();
                    connectedRegion?.Dispose();
                }
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