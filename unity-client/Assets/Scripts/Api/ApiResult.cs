namespace OmniScan.Api
{
    public enum ApiErrorKind
    {
        NotFound,
        ServerError,
        NetworkError,
    }

    public class ApiError
    {
        public ApiErrorKind Kind { get; }
        public string Code { get; }
        public string Message { get; }
        public long? StatusCode { get; }

        public ApiError(ApiErrorKind kind, string code, string message, long? statusCode = null)
        {
            Kind = kind;
            Code = code;
            Message = message;
            StatusCode = statusCode;
        }
    }

    public readonly struct ApiResult<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public ApiError Error { get; }

        private ApiResult(bool isSuccess, T value, ApiError error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static ApiResult<T> Success(T value) => new(true, value, null);
        public static ApiResult<T> Failure(ApiError error) => new(false, default, error);
    }
}
