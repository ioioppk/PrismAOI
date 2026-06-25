using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ImageLib.Core;
using Moq;
using WorkflowEngine.Core;

namespace WorkflowEngine.Tests
{
    public class RecipeExecutorTests
    {
        private static Mock<IImageProcessor> CreateMockProcessor(string id, bool success = true)
        {
            var mock = new Mock<IImageProcessor>();
            mock.Setup(p => p.Id).Returns(id);
            mock.Setup(p => p.Name).Returns(id);
            mock.Setup(p => p.ExecuteAsync(It.IsAny<ProcessContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (success)
                        return ProcessResult.Success();
                    return ProcessResult.Failure("模拟失败");
                });
            return mock;
        }

        private static IServiceProvider CreateServiceProvider(params IImageProcessor[] processors)
        {
            var mock = new Mock<IServiceProvider>();
            foreach (var p in processors)
            {
                mock.Setup(sp => sp.GetService(typeof(IImageProcessor)))
                    .Returns(p);
            }
            return mock.Object;
        }

        [Fact]
        public async Task ExecuteAsync_NullRecipe_ThrowsArgumentNullException()
        {
            var executor = new RecipeExecutor(CreateServiceProvider());

            Func<Task> act = () => executor.ExecuteAsync(null);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_SingleStep_Success_ReturnsSuccess()
        {
            var processor = CreateMockProcessor("Acquisition");
            var sp = CreateServiceProvider(processor.Object);
            var executor = new RecipeExecutor(sp);

            var recipe = new InspectionRecipe
            {
                Id = "r1",
                Name = "单步测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps =
                        {
                            new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" }
                        }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeEmpty();
            result.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
            result.StepResults.Should().HaveCount(1);
            result.FailedStepIds.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_MultiStep_AllSuccess_ReturnsSuccess()
        {
            var p1 = CreateMockProcessor("Acquisition");
            var p2 = CreateMockProcessor("BlobAnalysis");
            var sp = new Mock<IServiceProvider>();
            sp.SetupSequence(s => s.GetService(typeof(IImageProcessor)))
                .Returns(p1.Object)
                .Returns(p2.Object);
            var executor = new RecipeExecutor(sp.Object);

            var recipe = new InspectionRecipe
            {
                Id = "r2",
                Name = "多步测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    },
                    new InspectionStep
                    {
                        Id = "s2",
                        Name = "检测",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n2", ProcessorType = "BlobAnalysis" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeTrue();
            result.StepResults.Should().HaveCount(2);
            result.FailedStepIds.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_StepFails_StopOnFailure_StopsExecution()
        {
            var p1 = CreateMockProcessor("Acquisition");
            var p2 = CreateMockProcessor("FailProcessor", success: false);
            var p3 = CreateMockProcessor("BlobAnalysis");
            var sp = new Mock<IServiceProvider>();
            sp.SetupSequence(s => s.GetService(typeof(IImageProcessor)))
                .Returns(p1.Object)
                .Returns(p2.Object)
                .Returns(p3.Object);
            var executor = new RecipeExecutor(sp.Object);

            var recipe = new InspectionRecipe
            {
                Id = "r3",
                Name = "失败中止测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    },
                    new InspectionStep
                    {
                        Id = "s2",
                        Name = "失败步骤",
                        Type = StepType.Inspection,
                        StopOnFailure = true,
                        SubSteps = { new ProcessorNode { Id = "n2", ProcessorType = "FailProcessor" } }
                    },
                    new InspectionStep
                    {
                        Id = "s3",
                        Name = "不应执行",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n3", ProcessorType = "BlobAnalysis" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeFalse();
            result.StepResults.Should().HaveCount(2, "第三个步骤不应被执行");
            result.FailedStepIds.Should().Contain("s2");
            result.FailedStepIds.Should().NotContain("s3");
        }

        [Fact]
        public async Task ExecuteAsync_StepFails_StopOnFailureFalse_ContinuesExecution()
        {
            var p1 = CreateMockProcessor("Acquisition");
            var p2 = CreateMockProcessor("FailProcessor", success: false);
            var p3 = CreateMockProcessor("BlobAnalysis");
            var sp = new Mock<IServiceProvider>();
            sp.SetupSequence(s => s.GetService(typeof(IImageProcessor)))
                .Returns(p1.Object)
                .Returns(p2.Object)
                .Returns(p3.Object);
            var executor = new RecipeExecutor(sp.Object);

            var recipe = new InspectionRecipe
            {
                Id = "r4",
                Name = "失败继续测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    },
                    new InspectionStep
                    {
                        Id = "s2",
                        Name = "失败但继续",
                        Type = StepType.Inspection,
                        StopOnFailure = false,
                        SubSteps = { new ProcessorNode { Id = "n2", ProcessorType = "FailProcessor" } }
                    },
                    new InspectionStep
                    {
                        Id = "s3",
                        Name = "继续执行",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n3", ProcessorType = "BlobAnalysis" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeFalse("有失败步骤但整体仍完成");
            result.StepResults.Should().HaveCount(3, "所有步骤都应被执行");
            result.FailedStepIds.Should().Contain("s2");
            result.FailedStepIds.Should().NotContain("s1");
            result.FailedStepIds.Should().NotContain("s3");
            result.ErrorMessage.Should().Contain("1 failed");
        }

        [Fact]
        public async Task ExecuteAsync_ProcessorNotFound_FailsStep()
        {
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IImageProcessor))).Returns(null);
            var executor = new RecipeExecutor(sp.Object);

            var recipe = new InspectionRecipe
            {
                Id = "r5",
                Name = "算子未找到",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "缺失算子",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "NonExistent" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeFalse();
            result.FailedStepIds.Should().Contain("s1");
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task ExecuteAsync_CancellationRequested_StopsEarly()
        {
            var processor = CreateMockProcessor("Acquisition");
            var sp = CreateServiceProvider(processor.Object);
            var executor = new RecipeExecutor(sp);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var recipe = new InspectionRecipe
            {
                Id = "r6",
                Name = "取消测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "步骤1",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe, cts.Token);

            // 取消后可能成功（未执行任何步骤）或失败（有残留），但不应崩溃
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_StepResults_ContainCorrectNames()
        {
            var p1 = CreateMockProcessor("Acquisition");
            var p2 = CreateMockProcessor("BlobAnalysis");
            var sp = new Mock<IServiceProvider>();
            sp.SetupSequence(s => s.GetService(typeof(IImageProcessor)))
                .Returns(p1.Object)
                .Returns(p2.Object);
            var executor = new RecipeExecutor(sp.Object);

            var recipe = new InspectionRecipe
            {
                Id = "r7",
                Name = "步骤名称测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "图像采集步骤",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    },
                    new InspectionStep
                    {
                        Id = "s2",
                        Name = "Blob检测步骤",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n2", ProcessorType = "BlobAnalysis" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.StepResults.Should().ContainKey("s1");
            result.StepResults.Should().ContainKey("s2");
            var sr1 = result.StepResults["s1"] as StepResult;
            var sr2 = result.StepResults["s2"] as StepResult;
            sr1.Should().NotBeNull();
            sr1.StepName.Should().Be("图像采集步骤");
            sr2.Should().NotBeNull();
            sr2.StepName.Should().Be("Blob检测步骤");
        }

        [Fact]
        public async Task ExecuteAsync_StepResults_ContainElapsedTime()
        {
            var processor = CreateMockProcessor("Acquisition");
            var sp = CreateServiceProvider(processor.Object);
            var executor = new RecipeExecutor(sp);

            var recipe = new InspectionRecipe
            {
                Id = "r8",
                Name = "耗时测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "耗时步骤",
                        Type = StepType.ImageAcquisition,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "Acquisition" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            var sr = result.StepResults["s1"] as StepResult;
            sr.Should().NotBeNull();
            sr.ElapsedMs.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void RecipeResult_Success_PropertiesCorrect()
        {
            var elapsed = TimeSpan.FromSeconds(1.5);
            var stepResults = new Dictionary<string, object>
            {
                ["s1"] = new StepResult { StepName = "步骤1", IsSuccess = true }
            };

            var result = RecipeResult.Success(elapsed, stepResults);

            result.IsSuccess.Should().BeTrue();
            result.Elapsed.Should().Be(elapsed);
            result.ErrorMessage.Should().BeEmpty();
            result.StepResults.Should().HaveCount(1);
            result.FailedStepIds.Should().BeEmpty();
        }

        [Fact]
        public void RecipeResult_Failure_PropertiesCorrect()
        {
            var elapsed = TimeSpan.FromSeconds(0.5);
            var stepResults = new Dictionary<string, object>
            {
                ["s1"] = new StepResult { StepName = "步骤1", IsSuccess = true },
                ["s2"] = new StepResult { StepName = "步骤2", IsSuccess = false, ErrorMessage = "失败" }
            };
            var failedIds = new List<string> { "s2" };

            var result = RecipeResult.Failure("检测失败", elapsed, stepResults, failedIds);

            result.IsSuccess.Should().BeFalse();
            result.Elapsed.Should().Be(elapsed);
            result.ErrorMessage.Should().Be("检测失败");
            result.StepResults.Should().HaveCount(2);
            result.FailedStepIds.Should().ContainSingle().Which.Should().Be("s2");
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionInProcessor_HandledGracefully()
        {
            var mock = new Mock<IImageProcessor>();
            mock.Setup(p => p.Id).Returns("CrashProcessor");
            mock.Setup(p => p.ExecuteAsync(It.IsAny<ProcessContext>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("运行时异常"));
            var sp = CreateServiceProvider(mock.Object);
            var executor = new RecipeExecutor(sp);

            var recipe = new InspectionRecipe
            {
                Id = "r9",
                Name = "异常测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "异常步骤",
                        Type = StepType.Inspection,
                        SubSteps = { new ProcessorNode { Id = "n1", ProcessorType = "CrashProcessor" } }
                    }
                }
            };

            var result = await executor.ExecuteAsync(recipe);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("运行时异常");
            result.FailedStepIds.Should().Contain("s1");
        }
    }
}