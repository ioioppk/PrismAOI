using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ImageLib.Core;
using ImageLib.Operators;
using Xunit;

namespace ImageLib.Tests
{
    public class AdditionalProcessorsTests
    {
        [Fact]
        public async Task MeanFilter_ShouldFail_WhenInputImageIsMissing()
        {
            var processor = new MeanFilterProcessor();
            var context = new ProcessContext();

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("No input image found");
        }

        [Fact]
        public async Task MeanFilter_ShouldSucceed_WhenInputImageExists()
        {
            var processor = new MeanFilterProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["KernelWidth"] = 5;
            context["KernelHeight"] = 5;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            context.OutputImage.Should().BeSameAs(image);
            result.OutputValues.Should().ContainKey("KernelWidth");
            result.OutputValues.Should().ContainKey("KernelHeight");
        }

        [Fact]
        public async Task MedianFilter_ShouldExposeConfiguredMaskParameters()
        {
            var processor = new MedianFilterProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["MaskType"] = "circle";
            context["Radius"] = 2;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            result.OutputValues["MaskType"].Should().Be("circle");
            result.OutputValues["Radius"].Should().Be(2);
        }

        [Fact]
        public async Task GaussianFilter_ShouldExposeSigma()
        {
            var processor = new GaussianFilterProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["Sigma"] = 2.5;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            result.OutputValues["Sigma"].Should().Be(2.5);
        }

        [Fact]
        public async Task ImageEnhancement_ShouldExposeEnhancementParameters()
        {
            var processor = new ImageEnhancementProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["MaskWidth"] = 9;
            context["MaskHeight"] = 9;
            context["Factor"] = 1.8;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            result.OutputValues["Factor"].Should().Be(1.8);
        }

        [Fact]
        public async Task ShapeBasedMatching_ShouldReturnMatchSummary()
        {
            var processor = new ShapeBasedMatchingProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["MinScore"] = 0.8;
            context["Greediness"] = 0.9;
            context["NumMatches"] = 1;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            result.OutputValues.Should().ContainKey("MatchCount");
            result.OutputValues.Should().ContainKey("BestScore");
        }

        [Fact]
        public async Task AffineTransform_ShouldPreserveOutputImage()
        {
            var processor = new AffineTransformProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["TranslateX"] = 12.5;
            context["RotateDeg"] = 5.0;
            context["ScaleX"] = 1.1;
            context["ScaleY"] = 0.9;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            context.OutputImage.Should().BeSameAs(image);
            result.OutputValues["ScaleX"].Should().Be(1.1);
            result.OutputValues["ScaleY"].Should().Be(0.9);
        }

        [Fact]
        public async Task CaliperMeasurement_ShouldReturnDistanceAndToleranceFlag()
        {
            var processor = new CaliperMeasurementProcessor();
            var image = new FakeImageData();
            var context = new ProcessContext
            {
                InputImage = image
            };
            context["ExpectedWidth"] = 120.0;
            context["Tolerance"] = 5.0;

            var result = await processor.ExecuteAsync(context);

            result.IsSuccess.Should().BeTrue();
            result.OutputValues["MeasuredDistance"].Should().Be(120.0);
            result.OutputValues["IsInTolerance"].Should().Be(true);
        }

        private sealed class FakeImageData : IImageData
        {
            public string Id { get; } = Guid.NewGuid().ToString("N");
            public int Width { get; } = 640;
            public int Height { get; } = 480;
            public int Channels { get; } = 1;
            public string PixelType { get; } = "byte";
            public DateTime CaptureTime { get; } = DateTime.Now;
            public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

            public object GetNativeHandle()
            {
                return new object();
            }

            public IImageData Clone()
            {
                return new FakeImageData();
            }

            public void Dispose()
            {
            }
        }
    }
}
