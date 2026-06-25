namespace CommLib.Core
{
    /// <summary>
    /// 通讯结果
    /// </summary>
    public class CommunicationResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public byte[] Data { get; }

        protected CommunicationResult(bool isSuccess, string errorMessage, byte[] data)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
            Data = data;
        }

        public static CommunicationResult Success(byte[] data = null)
        {
            return new CommunicationResult(true, null, data);
        }

        public static CommunicationResult Failure(string errorMessage, byte[] data = null)
        {
            return new CommunicationResult(false, errorMessage, data);
        }
    }

    /// <summary>
    /// 泛型通讯结果
    /// </summary>
    public class CommunicationResult<T> : CommunicationResult
    {
        public T Value { get; }

        private CommunicationResult(bool isSuccess, string errorMessage, T value)
            : base(isSuccess, errorMessage, null)
        {
            Value = value;
        }

        public new static CommunicationResult<T> Success(T value)
        {
            return new CommunicationResult<T>(true, null, value);
        }

        public new static CommunicationResult<T> Failure(string errorMessage)
        {
            return new CommunicationResult<T>(false, errorMessage, default);
        }
    }

    /// <summary>
    /// 连接结果
    /// </summary>
    public class ConnectionResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private ConnectionResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static ConnectionResult Success()
        {
            return new ConnectionResult(true, null);
        }

        public static ConnectionResult Failure(string errorMessage)
        {
            return new ConnectionResult(false, errorMessage);
        }
    }
}
