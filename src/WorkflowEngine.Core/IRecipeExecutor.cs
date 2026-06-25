using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowEngine.Core
{
    /// <summary>
    /// 顶层：检测配方
    /// </summary>
    public class InspectionRecipe
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; } = "1.0";
        public List<InspectionStep> Steps { get; set; } = new List<InspectionStep>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 配方执行结果
    /// </summary>
    public class RecipeResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public TimeSpan Elapsed { get; set; }
        public Dictionary<string, object> StepResults { get; }
        public IReadOnlyList<string> FailedStepIds { get; }

        private RecipeResult(bool isSuccess, string errorMessage, TimeSpan elapsed,
            Dictionary<string, object> stepResults, IReadOnlyList<string> failedStepIds)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
            Elapsed = elapsed;
            StepResults = stepResults ?? new Dictionary<string, object>();
            FailedStepIds = failedStepIds ?? Array.Empty<string>();
        }

        public static RecipeResult Success(TimeSpan elapsed, Dictionary<string, object> stepResults)
        {
            return new RecipeResult(true, null, elapsed, stepResults, null);
        }

        public static RecipeResult Failure(string errorMessage, TimeSpan elapsed,
            Dictionary<string, object> stepResults, IReadOnlyList<string> failedStepIds)
        {
            return new RecipeResult(false, errorMessage, elapsed, stepResults, failedStepIds);
        }
    }

    /// <summary>
    /// 配方执行器接口
    /// </summary>
    public interface IRecipeExecutor
    {
        Task<RecipeResult> ExecuteAsync(InspectionRecipe recipe, CancellationToken ct = default);
    }
}
