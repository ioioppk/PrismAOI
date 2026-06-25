using System;
using System.Collections.Generic;

namespace ImageLib.Core
{
    /// <summary>
    /// 算子执行结果
    /// </summary>
    public class ProcessResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public TimeSpan Elapsed { get; set; }
        public IReadOnlyDictionary<string, object> OutputValues { get; }

        private ProcessResult(bool isSuccess, string errorMessage, IReadOnlyDictionary<string, object> outputValues)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
            OutputValues = outputValues ?? new Dictionary<string, object>();
        }

        public static ProcessResult Success(IReadOnlyDictionary<string, object> outputValues = null)
        {
            return new ProcessResult(true, null, outputValues);
        }

        public static ProcessResult Failure(string errorMessage, IReadOnlyDictionary<string, object> outputValues = null)
        {
            return new ProcessResult(false, errorMessage, outputValues);
        }
    }
}
