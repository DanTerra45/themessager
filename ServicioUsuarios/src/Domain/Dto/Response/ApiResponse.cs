namespace Domain.Dto.Response
{
    public abstract class ApiResponse
    {
        public bool IsSuccess { get; }
        public object? Data { get; }
        public ApiResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
        public static ApiResponse Failure(object? data = null) => new ApiResponseFailure(data);
        public static ApiResponse Ok(object? data = null) => new ApiResponseOk(data);
    }
    public class ApiResponseOk : ApiResponse
    {
        private readonly object? _data;
        public ApiResponseOk(object? data) : base(true)
        {
            _data = data;
        }
    }
    public class ApiResponseFailure : ApiResponse
    {
        private readonly object? _data;
        public ApiResponseFailure(object? data = null) : base(false)
        {
            _data = data;
        }
    }
}