namespace Mercadito.Users.Api.Domain.Shared
{
    public class Result
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public IReadOnlyDictionary<string, List<string>> Errors => _errors;

        public string ErrorMessage { get; }

        protected Result(bool isSuccess, string errorMessage, IReadOnlyDictionary<string, List<string>>? errors = null)
        {
            if (errorMessage == null)
            {
                errorMessage = string.Empty;
            }

            if (isSuccess && !string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException("Successful result must not contain an error message.", nameof(errorMessage));
            }

            if (!isSuccess && string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Failed result must contain a non-empty error message.", nameof(errorMessage));
            }

            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;

            if (errors == null)
            {
                return;
            }

            foreach (var error in errors)
            {
                _errors[error.Key] = [.. error.Value];
            }
        }

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Failure(string errorMessage)
        {
            if (errorMessage == null)
            {
                errorMessage = string.Empty;
            }

            return new Result(false, errorMessage);
        }

        public static Result Failure(IReadOnlyDictionary<string, List<string>> errors)
        {
            return new Result(false, BuildErrorMessage(errors), errors);
        }

        public static Result<T> Success<T>(T value)
        {
            return new Result<T>(true, value, string.Empty);
        }

        public static Result<T> Failure<T>(string errorMessage)
        {
            if (errorMessage == null)
            {
                errorMessage = string.Empty;
            }

            return new Result<T>(false, default!, errorMessage);
        }

        public static Result<T> Failure<T>(IReadOnlyDictionary<string, List<string>> errors)
        {
            return new Result<T>(false, default!, BuildErrorMessage(errors), errors);
        }

        protected static string BuildErrorMessage(IReadOnlyDictionary<string, List<string>> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            foreach (var error in errors)
            {
                if (error.Value.Count > 0 && !string.IsNullOrWhiteSpace(error.Value[0]))
                {
                    return error.Value[0];
                }
            }

            return "La operación no pudo completarse por errores de validación.";
        }

        public void Deconstruct(out bool isSuccess, out string errorMessage)
        {
            isSuccess = IsSuccess;
            errorMessage = ErrorMessage;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return "Result: Success";
            }

            return $"Result: Failure - {ErrorMessage}";
        }
    }

    public class Result<T> : Result
    {
        private readonly T _value;

        public T Value
        {
            get
            {
                if (IsFailure)
                {
                    throw new InvalidOperationException("Cannot access Value when result is a failure. Use TryGetValue instead.");
                }

                return _value;
            }
        }

        public T ValueOrDefault
        {
            get
            {
                if (IsSuccess)
                {
                    return _value;
                }

                return default!;
            }
        }

        protected internal Result(bool isSuccess, T value, string errorMessage, IReadOnlyDictionary<string, List<string>>? errors = null)
            : base(isSuccess, errorMessage, errors)
        {
            _value = value!;
        }

        public bool TryGetValue(out T value)
        {
            if (IsSuccess)
            {
                value = _value!;
                return true;
            }

            value = default!;
            return false;
        }

        public void Deconstruct(out T value, out bool isSuccess, out string errorMessage)
        {
            value = ValueOrDefault;
            isSuccess = IsSuccess;
            errorMessage = ErrorMessage;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return $"Result<{typeof(T).Name}>: Success";
            }

            return $"Result<{typeof(T).Name}>: Failure - {ErrorMessage}";
        }
    }
}
