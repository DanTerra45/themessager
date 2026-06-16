namespace ServicioReportes.Domain.Shared
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
            errorMessage ??= string.Empty;

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

            if (errors is null)
            {
                return;
            }

            foreach (var error in errors)
            {
                _errors[error.Key] = [.. error.Value];
            }
        }

        public static Result Success() => new(true, string.Empty);
        public static Result Failure(string errorMessage) => new(false, errorMessage ?? string.Empty);
        public static Result Failure(IReadOnlyDictionary<string, List<string>> errors) => new(false, BuildErrorMessage(errors), errors);
        public static Result<T> Success<T>(T value) => new(true, value, string.Empty);
        public static Result<T> Failure<T>(string errorMessage) => new(false, default!, errorMessage ?? string.Empty);
        public static Result<T> Failure<T>(IReadOnlyDictionary<string, List<string>> errors) => new(false, default!, BuildErrorMessage(errors), errors);

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
                    throw new InvalidOperationException("Cannot access Value when result is a failure.");
                }

                return _value;
            }
        }

        protected internal Result(bool isSuccess, T value, string errorMessage, IReadOnlyDictionary<string, List<string>>? errors = null)
            : base(isSuccess, errorMessage, errors)
        {
            _value = value!;
        }
    }
}
