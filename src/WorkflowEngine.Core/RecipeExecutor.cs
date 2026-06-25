using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageLib.Core;

namespace WorkflowEngine.Core
{
    public sealed class RecipeExecutor : IRecipeExecutor
    {
        private readonly IServiceProvider _serviceProvider;

        public RecipeExecutor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<RecipeResult> ExecuteAsync(InspectionRecipe recipe, CancellationToken ct = default)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            var sw = Stopwatch.StartNew();
            var stepResults = new Dictionary<string, StepResult>();
            var failedStepIds = new List<string>();
            var context = new ProcessContext();

            foreach (var step in recipe.Steps)
            {
                if (ct.IsCancellationRequested) break;

                var stepSw = Stopwatch.StartNew();
                var stepResult = new StepResult { StepName = step.Name };

                try
                {
                    foreach (var subStep in step.SubSteps)
                    {
                        if (ct.IsCancellationRequested) break;

                        var processor = _serviceProvider.GetService(typeof(IImageProcessor)) as IImageProcessor;
                        if (processor == null || processor.Id != subStep.ProcessorType)
                        {
                            stepResult.IsSuccess = false;
                            stepResult.ErrorMessage = $"Processor '{subStep.ProcessorType}' not found.";
                            failedStepIds.Add(step.Id);
                            if (step.StopOnFailure)
                            {
                                stepSw.Stop();
                                stepResult.ElapsedMs = stepSw.ElapsedMilliseconds;
                                stepResults[step.Id] = stepResult;
                                sw.Stop();
                                return RecipeResult.Failure(stepResult.ErrorMessage, sw.Elapsed, ToObjectDict(stepResults), failedStepIds);
                            }
                            break;
                        }

                        foreach (var kvp in subStep.Parameters)
                        {
                            context[kvp.Key] = kvp.Value;
                        }

                        var subResult = await processor.ExecuteAsync(context, ct);

                        if (!subResult.IsSuccess)
                        {
                            stepResult.IsSuccess = false;
                            stepResult.ErrorMessage = subResult.ErrorMessage;
                            failedStepIds.Add(step.Id);

                            if (step.StopOnFailure)
                            {
                                stepSw.Stop();
                                stepResult.ElapsedMs = stepSw.ElapsedMilliseconds;
                                stepResults[step.Id] = stepResult;
                                sw.Stop();
                                return RecipeResult.Failure(subResult.ErrorMessage, sw.Elapsed, ToObjectDict(stepResults), failedStepIds);
                            }
                        }
                        else
                        {
                            stepResult.IsSuccess = true;
                            foreach (var kv in subResult.OutputValues)
                            {
                                stepResult.Outputs[kv.Key] = kv.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    stepResult.IsSuccess = false;
                    stepResult.ErrorMessage = ex.Message;
                    failedStepIds.Add(step.Id);

                    if (step.StopOnFailure)
                    {
                        stepSw.Stop();
                        stepResult.ElapsedMs = stepSw.ElapsedMilliseconds;
                        stepResults[step.Id] = stepResult;
                        sw.Stop();
                        return RecipeResult.Failure(ex.Message, sw.Elapsed, ToObjectDict(stepResults), failedStepIds);
                    }
                }

                stepSw.Stop();
                stepResult.ElapsedMs = stepSw.ElapsedMilliseconds;
                stepResults[step.Id] = stepResult;
            }

            sw.Stop();

            if (failedStepIds.Count > 0)
            {
                return RecipeResult.Failure(
                    $"Recipe completed with {failedStepIds.Count} failed step(s).",
                    sw.Elapsed, ToObjectDict(stepResults), failedStepIds);
            }

            return RecipeResult.Success(sw.Elapsed, ToObjectDict(stepResults));
        }

        private static Dictionary<string, object> ToObjectDict(Dictionary<string, StepResult> dict)
        {
            var result = new Dictionary<string, object>();
            foreach (var kv in dict)
                result[kv.Key] = kv.Value;
            return result;
        }
    }
}