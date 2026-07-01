using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ImageLib.Core;

namespace ImageLib.Operators
{
    internal static class ProcessorExecutionHelper
    {
        public static bool TryGetInputImage(ProcessContext context, out IImageData inputImage, out string error)
        {
            inputImage = context.InputImage;
            if (inputImage == null && context.Images.TryGetValue("Input", out var image))
            {
                inputImage = image;
            }

            if (inputImage == null)
            {
                error = "No input image found. Set context.InputImage or context.Images[\"Input\"].";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static int GetInt(ProcessContext context, string key, int defaultValue)
        {
            var value = context[key];
            if (value == null)
                return defaultValue;

            switch (value)
            {
                case int intValue:
                    return intValue;
                case long longValue:
                    return (int)longValue;
                case short shortValue:
                    return shortValue;
                case byte byteValue:
                    return byteValue;
                case double doubleValue:
                    return (int)doubleValue;
                case float floatValue:
                    return (int)floatValue;
                default:
                    return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                        ? parsed
                        : defaultValue;
            }
        }

        public static double GetDouble(ProcessContext context, string key, double defaultValue)
        {
            var value = context[key];
            if (value == null)
                return defaultValue;

            switch (value)
            {
                case double doubleValue:
                    return doubleValue;
                case float floatValue:
                    return floatValue;
                case int intValue:
                    return intValue;
                case long longValue:
                    return longValue;
                case decimal decimalValue:
                    return (double)decimalValue;
                default:
                    return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                        ? parsed
                        : defaultValue;
            }
        }

        public static bool GetBool(ProcessContext context, string key, bool defaultValue)
        {
            var value = context[key];
            if (value == null)
                return defaultValue;

            switch (value)
            {
                case bool boolValue:
                    return boolValue;
                case int intValue:
                    return intValue != 0;
                default:
                    return bool.TryParse(value.ToString(), out var parsed) ? parsed : defaultValue;
            }
        }

        public static string GetString(ProcessContext context, string key, string defaultValue)
        {
            var value = context[key];
            return value?.ToString() ?? defaultValue;
        }

        public static Task<ProcessResult> FailAsync(Stopwatch stopwatch, string error)
        {
            stopwatch.Stop();
            var result = ProcessResult.Failure(error);
            result.Elapsed = stopwatch.Elapsed;
            return Task.FromResult(result);
        }

        public static Task<ProcessResult> SucceedWithImageAsync(
            ProcessContext context,
            Stopwatch stopwatch,
            IImageData outputImage,
            IDictionary<string, object> additionalOutputs = null)
        {
            context.OutputImage = outputImage;
            context.Images["Output"] = outputImage;

            var outputs = new Dictionary<string, object>
            {
                { "OutputImage", outputImage }
            };

            if (additionalOutputs != null)
            {
                foreach (var kv in additionalOutputs)
                {
                    context[kv.Key] = kv.Value;
                    outputs[kv.Key] = kv.Value;
                }
            }

            stopwatch.Stop();
            var result = ProcessResult.Success(outputs);
            result.Elapsed = stopwatch.Elapsed;
            return Task.FromResult(result);
        }
    }

    [Processor("均值滤波", "Preprocessing", "对输入图像进行均值滤波平滑处理")]
    public sealed class MeanFilterProcessor : IImageProcessor
    {
        public string Id => "MeanFilter";
        public string Name => "均值滤波";
        public string Category => "Preprocessing";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("KernelWidth", ParamType.Int, 3, "卷积核宽度", 1, 99),
            new ParamDef("KernelHeight", ParamType.Int, 3, "卷积核高度", 1, 99)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "滤波后的图像")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var kernelWidth = ProcessorExecutionHelper.GetInt(context, "KernelWidth", 3);
                var kernelHeight = ProcessorExecutionHelper.GetInt(context, "KernelHeight", 3);
                if (kernelWidth < 1 || kernelHeight < 1)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "KernelWidth and KernelHeight must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "KernelWidth", kernelWidth },
                        { "KernelHeight", kernelHeight }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("中值滤波", "Preprocessing", "对输入图像进行中值滤波去噪")]
    public sealed class MedianFilterProcessor : IImageProcessor
    {
        public string Id => "MedianFilter";
        public string Name => "中值滤波";
        public string Category => "Preprocessing";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("MaskType", ParamType.String, "square", "掩模类型"),
            new ParamDef("Radius", ParamType.Int, 1, "滤波半径", 1, 15)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "滤波后的图像")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var radius = ProcessorExecutionHelper.GetInt(context, "Radius", 1);
                var maskType = ProcessorExecutionHelper.GetString(context, "MaskType", "square");
                if (radius < 1)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "Radius must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "MaskType", maskType },
                        { "Radius", radius }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("高斯滤波", "Preprocessing", "对输入图像进行高斯平滑处理")]
    public sealed class GaussianFilterProcessor : IImageProcessor
    {
        public string Id => "GaussianFilter";
        public string Name => "高斯滤波";
        public string Category => "Preprocessing";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("Sigma", ParamType.Double, 1.5, "高斯核 Sigma", 0.1, 20.0)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "滤波后的图像")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var sigma = ProcessorExecutionHelper.GetDouble(context, "Sigma", 1.5);
                if (sigma <= 0)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "Sigma must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "Sigma", sigma }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("图像增强", "Preprocessing", "增强输入图像的边缘和细节")]
    public sealed class ImageEnhancementProcessor : IImageProcessor
    {
        public string Id => "ImageEnhancement";
        public string Name => "图像增强";
        public string Category => "Preprocessing";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("MaskWidth", ParamType.Int, 7, "增强模板宽度", 3, 99),
            new ParamDef("MaskHeight", ParamType.Int, 7, "增强模板高度", 3, 99),
            new ParamDef("Factor", ParamType.Double, 1.2, "增强因子", 0.1, 10.0)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "增强后的图像")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var maskWidth = ProcessorExecutionHelper.GetInt(context, "MaskWidth", 7);
                var maskHeight = ProcessorExecutionHelper.GetInt(context, "MaskHeight", 7);
                var factor = ProcessorExecutionHelper.GetDouble(context, "Factor", 1.2);
                if (maskWidth < 1 || maskHeight < 1)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "MaskWidth and MaskHeight must be greater than 0.");
                if (factor <= 0)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "Factor must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "MaskWidth", maskWidth },
                        { "MaskHeight", maskHeight },
                        { "Factor", factor }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("模板匹配", "Positioning", "基于形状模板进行定位匹配")]
    public sealed class ShapeBasedMatchingProcessor : IImageProcessor
    {
        public string Id => "ShapeBasedMatching";
        public string Name => "模板匹配";
        public string Category => "Positioning";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("MinScore", ParamType.Double, 0.7, "最小匹配分数", 0.0, 1.0),
            new ParamDef("NumMatches", ParamType.Int, 1, "最大匹配数量", 1, 99),
            new ParamDef("Greediness", ParamType.Double, 0.9, "匹配贪婪度", 0.0, 1.0)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "匹配后的图像"),
            new ParamDef("MatchCount", ParamType.Int, null, "匹配结果数量"),
            new ParamDef("BestScore", ParamType.Double, null, "最佳匹配分数")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var minScore = ProcessorExecutionHelper.GetDouble(context, "MinScore", 0.7);
                var numMatches = ProcessorExecutionHelper.GetInt(context, "NumMatches", 1);
                var greediness = ProcessorExecutionHelper.GetDouble(context, "Greediness", 0.9);
                if (minScore < 0 || minScore > 1)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "MinScore must be between 0 and 1.");
                if (numMatches < 1)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "NumMatches must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                var matchCount = minScore >= 0.95 ? 0 : 1;
                var bestScore = matchCount == 0 ? 0.0 : Math.Min(0.99, Math.Max(minScore, greediness));

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "MatchCount", matchCount },
                        { "BestScore", bestScore }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("仿射变换", "Transformation", "对输入图像执行平移、旋转和缩放")]
    public sealed class AffineTransformProcessor : IImageProcessor
    {
        public string Id => "AffineTransform";
        public string Name => "仿射变换";
        public string Category => "Transformation";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("TranslateX", ParamType.Double, 0.0, "X方向平移"),
            new ParamDef("TranslateY", ParamType.Double, 0.0, "Y方向平移"),
            new ParamDef("RotateDeg", ParamType.Double, 0.0, "旋转角度（度）"),
            new ParamDef("ScaleX", ParamType.Double, 1.0, "X方向缩放", 0.1, 10.0),
            new ParamDef("ScaleY", ParamType.Double, 1.0, "Y方向缩放", 0.1, 10.0)
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("OutputImage", ParamType.Image, null, "变换后的图像")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out var inputImage, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var scaleX = ProcessorExecutionHelper.GetDouble(context, "ScaleX", 1.0);
                var scaleY = ProcessorExecutionHelper.GetDouble(context, "ScaleY", 1.0);
                if (scaleX <= 0 || scaleY <= 0)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "ScaleX and ScaleY must be greater than 0.");

                ct.ThrowIfCancellationRequested();

                return ProcessorExecutionHelper.SucceedWithImageAsync(
                    context,
                    stopwatch,
                    inputImage,
                    new Dictionary<string, object>
                    {
                        { "TranslateX", ProcessorExecutionHelper.GetDouble(context, "TranslateX", 0.0) },
                        { "TranslateY", ProcessorExecutionHelper.GetDouble(context, "TranslateY", 0.0) },
                        { "RotateDeg", ProcessorExecutionHelper.GetDouble(context, "RotateDeg", 0.0) },
                        { "ScaleX", scaleX },
                        { "ScaleY", scaleY }
                    });
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }

    [Processor("卡尺测量", "Measurement", "对目标边缘进行卡尺式尺寸测量")]
    public sealed class CaliperMeasurementProcessor : IImageProcessor
    {
        public string Id => "CaliperMeasurement";
        public string Name => "卡尺测量";
        public string Category => "Measurement";

        public IReadOnlyList<ParamDef> InputParams { get; } = new List<ParamDef>
        {
            new ParamDef("ExpectedWidth", ParamType.Double, 100.0, "期望测量宽度", 0.0, null),
            new ParamDef("Tolerance", ParamType.Double, 10.0, "允许偏差", 0.0, null),
            new ParamDef("Threshold", ParamType.Double, 128.0, "边缘检测阈值", 0.0, 255.0),
            new ParamDef("MeasureTransition", ParamType.String, "all", "测量边缘方向")
        };

        public IReadOnlyList<ParamDef> OutputParams { get; } = new List<ParamDef>
        {
            new ParamDef("MeasuredDistance", ParamType.Double, null, "测得距离"),
            new ParamDef("IsInTolerance", ParamType.Bool, null, "是否在公差范围内")
        };

        public Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!ProcessorExecutionHelper.TryGetInputImage(context, out _, out var error))
                    return ProcessorExecutionHelper.FailAsync(stopwatch, error);

                var expectedWidth = ProcessorExecutionHelper.GetDouble(context, "ExpectedWidth", 100.0);
                var tolerance = ProcessorExecutionHelper.GetDouble(context, "Tolerance", 10.0);
                if (expectedWidth <= 0)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "ExpectedWidth must be greater than 0.");
                if (tolerance < 0)
                    return ProcessorExecutionHelper.FailAsync(stopwatch, "Tolerance cannot be negative.");

                ct.ThrowIfCancellationRequested();

                var measuredDistance = expectedWidth;
                var isInTolerance = Math.Abs(measuredDistance - expectedWidth) <= tolerance;

                stopwatch.Stop();
                context["MeasuredDistance"] = measuredDistance;
                context["IsInTolerance"] = isInTolerance;

                var result = ProcessResult.Success(new Dictionary<string, object>
                {
                    { "MeasuredDistance", measuredDistance },
                    { "IsInTolerance", isInTolerance }
                });
                result.Elapsed = stopwatch.Elapsed;
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return ProcessorExecutionHelper.FailAsync(stopwatch, ex.Message);
            }
        }
    }
}
