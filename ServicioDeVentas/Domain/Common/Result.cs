using System.Collections;

namespace Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    NotImplemented,
    Conflict,
    Internal,
    Forbidden,
    Unauthorized
}

public sealed record AppError(
    string Code,
    string Message,
    ErrorType Type,
    string? Field = null);

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyCollection<AppError> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyCollection<AppError>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<AppError>();
    }

    public static Result Success() => new(true);
    public static Result Failure(AppError error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<AppError> errors) => new(false, errors.ToArray());
    public static Result Validation(string field, string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Validation, field));
    public static Result NotFound(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.NotFound));
    public static Result NotImplemented(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.NotImplemented));
    public static Result Internal(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Internal));
    public static Result Forbidden(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Forbidden));
    public static Result Unauthorized(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Unauthorized));
}

public class Result<T> : Result
{
    public T Value { get; }

    protected Result(bool isSuccess, T value, IReadOnlyCollection<AppError>? errors = null) : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(AppError error) => new(false, default!, new[] { error });
    public static new Result<T> Failure(IEnumerable<AppError> errors) => new(false, default!, errors.ToArray());
    public static new Result<T> Validation(string field, string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Validation, field));
    public static new Result<T> NotFound(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.NotFound));
    public static new Result<T> NotImplemented(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.NotImplemented));
    public static new Result<T> Internal(string code, string message) =>
        Failure(new AppError(code, message, ErrorType.Internal));
}
