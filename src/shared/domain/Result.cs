using System;

namespace Shared.Domain
{
    /// <summary>
    /// Represents the outcome of an operation or validation.
    /// Immutable, framework-agnostic and suitable for domain use.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// True when operation succeeded.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// True when operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Non-empty error message when <see cref="IsFailure"/> is true.
        /// Empty string when <see cref="IsSuccess"/> is true.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Protected constructor enforces invariants for derived types.
        /// Use the static factory methods to create instances.
        /// </summary>
        /// <param name="isSuccess">Indicates success.</param>
        /// <param name="errorMessage">Error message or empty for success.</param>
        /// <exception cref="ArgumentException">Thrown when invariants are violated.</exception>
        protected Result(bool isSuccess, string errorMessage)
        {
            // Normalize null to empty to avoid nulls leaking.
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
        }

        /// <summary>
        /// Create a successful Result.
        /// </summary>
        public static Result Success() => new Result(true, string.Empty);

        /// <summary>
        /// Create a failed Result with a non-empty error message.
        /// </summary>
        /// <param name="errorMessage">Non-empty error message.</param>
        /// <returns>Failed <see cref="Result"/>.</returns>
        public static Result Failure(string errorMessage) => new Result(false, errorMessage ?? string.Empty);

        /// <summary>
        /// Deconstruct support for convenient usage: (isSuccess, errorMessage) = result;
        /// </summary>
        public void Deconstruct(out bool isSuccess, out string errorMessage)
        {
            isSuccess = IsSuccess;
            errorMessage = ErrorMessage;
        }

        /// <summary>
        /// Returns a string representation useful in diagnostics but not for logic.
        /// </summary>
        public override string ToString() =>
            IsSuccess ? "Result: Success" : $"Result: Failure - {ErrorMessage}";
    }

    /// <summary>
    /// Generic Result that carries a value when successful.
    /// Inherits behavior from <see cref="Result"/> so it can be used polymorphically.
    /// </summary>
    /// <typeparam name="T">Type of the value carried on success.</typeparam>
    public class Result<T> : Result
    {
        private readonly T _value;

        /// <summary>
        /// Value when <see cref="IsSuccess"/> is true.
        /// Accessing this when the result is a failure will throw.
        /// Use <see cref="TryGetValue(out T)"/> to avoid exceptions.
        /// </summary>
        /// <exception cref="InvalidOperationException">If result is a failure.</exception>
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

        /// <summary>
        /// Returns the value when success, otherwise default(T).
        /// Safe alternative to accessing <see cref="Value"/>.
        /// </summary>
        public T ValueOrDefault => IsSuccess ? _value : default!;

        /// <summary>
        /// Internal constructor enforces invariants via base ctor.
        /// </summary>
        protected internal Result(bool isSuccess, T value, string errorMessage)
            : base(isSuccess, errorMessage)
        {
            _value = value!;
        }

        /// <summary>
        /// Create a successful Result with a value.
        /// </summary>
        public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);

        /// <summary>
        /// Create a failed Result with a non-empty error message.
        /// Value is ignored and will be default(T).
        /// </summary>
        public static Result<T> Failure(string errorMessage) => new Result<T>(false, default!, errorMessage ?? string.Empty);

        /// <summary>
        /// Try to get the value; returns true if the result is success and outputs the value.
        /// Avoids throwing when trying to access the value of a failed result.
        /// </summary>
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

        /// <summary>
        /// Deconstruct to (value, isSuccess, errorMessage).
        /// When deconstructed, value is default(T) for failures.
        /// </summary>
        public void Deconstruct(out T value, out bool isSuccess, out string errorMessage)
        {
            value = ValueOrDefault;
            isSuccess = IsSuccess;
            errorMessage = ErrorMessage;
        }

        /// <summary>
        /// Friendly diagnostic representation.
        /// </summary>
        public override string ToString() =>
            IsSuccess ? $"Result<{typeof(T).Name}>: Success" : $"Result<{typeof(T).Name}>: Failure - {ErrorMessage}";
    }
}