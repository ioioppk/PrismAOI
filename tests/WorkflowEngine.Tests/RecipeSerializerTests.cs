using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using WorkflowEngine.Core;

namespace WorkflowEngine.Tests
{
    public class RecipeSerializerTests
    {
        [Fact]
        public void Serialize_ValidRecipe_ReturnsValidJson()
        {
            var recipe = new InspectionRecipe
            {
                Id = "test_001",
                Name = "测试配方",
                Version = "1.0",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "step_1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps =
                        {
                            new ProcessorNode
                            {
                                Id = "node_1",
                                ProcessorType = "Acquisition",
                                Parameters = { ["ImagePath"] = "C:\\test.bmp" }
                            }
                        }
                    }
                }
            };

            var json = RecipeSerializer.Serialize(recipe);

            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("\"id\"");
            json.Should().Contain("\"test_001\"");
            json.Should().Contain("\"step_1\"");
            json.Should().Contain("\"Acquisition\"");
        }

        [Fact]
        public void Deserialize_ValidJson_ReturnsRecipe()
        {
            var json = @"{
                ""id"": ""test_002"",
                ""name"": ""反序列化测试"",
                ""version"": ""2.0"",
                ""steps"": [
                    {
                        ""id"": ""step_a"",
                        ""name"": ""步骤A"",
                        ""type"": ""Inspection"",
                        ""stopOnFailure"": false,
                        ""subSteps"": [
                            {
                                ""id"": ""node_a1"",
                                ""processorType"": ""BlobAnalysis"",
                                ""parameters"": { ""Threshold"": 128 }
                            }
                        ]
                    }
                ]
            }";

            var recipe = RecipeSerializer.Deserialize(json);

            recipe.Should().NotBeNull();
            recipe.Id.Should().Be("test_002");
            recipe.Name.Should().Be("反序列化测试");
            recipe.Version.Should().Be("2.0");
            recipe.Steps.Should().HaveCount(1);
            recipe.Steps[0].Id.Should().Be("step_a");
            recipe.Steps[0].StopOnFailure.Should().BeFalse();
            recipe.Steps[0].SubSteps.Should().HaveCount(1);
            recipe.Steps[0].SubSteps[0].ProcessorType.Should().Be("BlobAnalysis");
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip_PreservesData()
        {
            var original = new InspectionRecipe
            {
                Id = "roundtrip_001",
                Name = "往返测试",
                Version = "1.5",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "s1",
                        Name = "采集",
                        Type = StepType.ImageAcquisition,
                        SubSteps =
                        {
                            new ProcessorNode
                            {
                                Id = "n1",
                                ProcessorType = "Acquisition",
                                Parameters = { ["ImagePath"] = "C:\\img.bmp" }
                            }
                        }
                    },
                    new InspectionStep
                    {
                        Id = "s2",
                        Name = "检测",
                        Type = StepType.Inspection,
                        StopOnFailure = false,
                        SubSteps =
                        {
                            new ProcessorNode
                            {
                                Id = "n2",
                                ProcessorType = "BlobAnalysis",
                                Parameters =
                                {
                                    ["MinArea"] = 100,
                                    ["MaxArea"] = 999999,
                                    ["Threshold"] = 200
                                }
                            }
                        }
                    }
                }
            };

            var json = RecipeSerializer.Serialize(original);
            var restored = RecipeSerializer.Deserialize(json);

            restored.Should().NotBeNull();
            restored.Id.Should().Be(original.Id);
            restored.Name.Should().Be(original.Name);
            restored.Version.Should().Be(original.Version);
            restored.Steps.Should().HaveCount(2);
            restored.Steps[0].Id.Should().Be("s1");
            restored.Steps[1].Id.Should().Be("s2");
            restored.Steps[1].StopOnFailure.Should().BeFalse();
            restored.Steps[1].SubSteps[0].Parameters["Threshold"].ToString().Should().Be("200");
        }

        [Fact]
        public void Deserialize_InvalidJson_ReturnsNull()
        {
            var result = RecipeSerializer.Deserialize("not valid json {{{");

            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsNull()
        {
            var result = RecipeSerializer.Deserialize("");

            result.Should().BeNull();
        }

        [Fact]
        public void Serialize_EmptySteps_ProducesEmptyArray()
        {
            var recipe = new InspectionRecipe
            {
                Id = "empty",
                Name = "空配方"
            };

            var json = RecipeSerializer.Serialize(recipe);

            json.Should().Contain("\"steps\"");
            json.Should().Contain("[]");
        }

        [Fact]
        public async Task SerializeToFile_And_DeserializeFromFile_RoundTrip()
        {
            var recipe = new InspectionRecipe
            {
                Id = "file_001",
                Name = "文件测试",
                Steps =
                {
                    new InspectionStep
                    {
                        Id = "fs1",
                        Name = "文件步骤",
                        Type = StepType.ImageAcquisition,
                        SubSteps =
                        {
                            new ProcessorNode { Id = "fn1", ProcessorType = "Acquisition" }
                        }
                    }
                }
            };

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_recipe_{Guid.NewGuid():N}.json");
            try
            {
                await RecipeSerializer.SerializeToFileAsync(tempFile, recipe);
                File.Exists(tempFile).Should().BeTrue();

                var restored = await RecipeSerializer.DeserializeFromFileAsync(tempFile);
                restored.Should().NotBeNull();
                restored.Id.Should().Be("file_001");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}