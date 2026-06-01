namespace Domain.Dto.Response
{
    public abstract class ApiResponse
    {
        public bool IsSuccess { get; }
        public object? Data { get; protected set; }
        public ApiResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
        public static ApiResponse Failure(object? data = null) => new ApiResponseFailure(data);
        public static ApiResponse Ok(object? data = null) => new ApiResponseOk(data);
    }
    public class ApiResponseOk : ApiResponse
    {
        public ApiResponseOk(object? data) : base(true)
        {
            Data = data;
        }
    }
    public class ApiResponseFailure : ApiResponse
    {
        public ApiResponseFailure(object? data = null) : base(false)
        {
            Data = data;
        }
    }
}