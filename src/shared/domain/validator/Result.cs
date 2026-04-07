namespace Mercadito.src.shared.domain.validator
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Dictionary<string, List<string>> Errors { get; } = new();

        protected Result(bool isSuccess) => IsSuccess = isSuccess;
        
        public static Result Success() => new(true);
        
        public static Result Failure(Dictionary<string, List<string>> errors)
        {
            var result = new Result(false);
            foreach (var error in errors)
            {
                result.Errors[error.Key] = error.Value;
            }
            return result;
        }

        public static Result Failure(params string[] errors)
        {
            var result = new Result(false);
            foreach (var error in errors)
            {
                result.Errors[$"_{result.Errors.Count}"] = [error];
            }
            return result;
        }
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value) : base(isSuccess) => Value = value;

        public static Result<T> Success(T value) => new(true, value);

        public static new Result<T> Failure(Dictionary<string, List<string>> errors)
        {
            var result = new Result<T>(false, default);
            foreach (var error in errors)
            {
                result.Errors[error.Key] = error.Value;
            }
            return result;
        }

        public static new Result<T> Failure(params string[] errors)
        {
            var result = new Result<T>(false, default);
            foreach (var error in errors)
            {
                result.Errors[$"_{result.Errors.Count}"] = [error];
            }
            return result;
        }
    }
}