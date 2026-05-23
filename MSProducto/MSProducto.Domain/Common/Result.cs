namespace MSProducto.Domain.Common
{
    public class Result
    {
        private readonly Dictionary<string, List<string>> _errors = [];
        private readonly List<string>? _validationErrors;

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public IReadOnlyDictionary<string, List<string>> Errors => _errors;

        public IReadOnlyList<string>? ValidationErrors => _validationErrors;

        public string Error { get; }

        protected Result(bool isSuccess, string error, IReadOnlyDictionary<string, List<string>>? errors = null, IReadOnlyList<string>? validationErrors = null)
        {
            if (error == null)
            {
                error = string.Empty;
            }

            if (isSuccess && !string.IsNullOrEmpty(error))
            {
                throw new ArgumentException("Successful result must not contain an error message.", nameof(error));
            }

            if (!isSuccess && string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentException("Failed result must contain a non-empty error message.", nameof(error));
            }

            IsSuccess = isSuccess;
            Error = error;
            _validationErrors = validationErrors != null ? new List<string>(validationErrors) : null;

            if (errors == null)
            {
                return;
            }

            foreach (var errorItem in errors)
            {
                _errors[errorItem.Key] = [.. errorItem.Value];
            }
        }

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Failure(string error)
        {
            if (error == null)
            {
                error = string.Empty;
            }

            return new Result(false, error);
        }

        public static Result Failure(string error, IReadOnlyList<string> validationErrors)
        {
            return new Result(false, error, validationErrors: validationErrors);
        }

        public static Result Failure(IReadOnlyDictionary<string, List<string>> errors)
        {
            return new Result(false, BuildErrorMessage(errors), errors);
        }

        public static Result<T> Success<T>(T value)
        {
            return new Result<T>(true, value, string.Empty);
        }

        public static Result<T> Failure<T>(string error)
        {
            if (error == null)
            {
                error = string.Empty;
            }

            return new Result<T>(false, default!, error);
        }

        public static Result<T> Failure<T>(string error, IReadOnlyList<string> validationErrors)
        {
            return new Result<T>(false, default!, error, validationErrors: validationErrors);
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

        public void Deconstruct(out bool isSuccess, out string error)
        {
            isSuccess = IsSuccess;
            error = Error;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return "Result: Success";
            }

            return $"Result: Failure - {Error}";
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

        protected internal Result(bool isSuccess, T value, string error, IReadOnlyDictionary<string, List<string>>? errors = null, IReadOnlyList<string>? validationErrors = null)
            : base(isSuccess, error, errors, validationErrors)
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

        public void Deconstruct(out T value, out bool isSuccess, out string error)
        {
            value = ValueOrDefault;
            isSuccess = IsSuccess;
            error = Error;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return $"Result<{typeof(T).Name}>: Success";
            }

            return $"Result<{typeof(T).Name}>: Failure - {Error}";
        }
    }
}