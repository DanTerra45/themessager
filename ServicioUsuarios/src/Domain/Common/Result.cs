using System.Collections;

namespace Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    private readonly Dictionary<string, List<string>> _errors = new();
    public IReadOnlyDictionary<string, IEnumerable<string>> Errors
        => _errors.ToDictionary(k => k.Key, v => (IEnumerable<string>)v.Value);

    protected Result(bool isSuccess) => IsSuccess = isSuccess;

    public static Result Success() => new(true);
    public static Result Failure(string key, string message)
    {
        var r = new Result(false);
        r._errors[key] = new() { message };
        return r;
    }
    public static Result Failure(Dictionary<string, IEnumerable<string>> errors)
    {
        var r = new Result(false);
        foreach (var kv in errors) r._errors[kv.Key] = kv.Value.ToList();
        return r;
    }
    public void AddError(string key, string message)
    {
        if (!_errors.ContainsKey(key)) _errors[key] = new();
        _errors[key].Add(message);
    }
}

public class Result<T> : Result
{
    public bool IsSuccess { get; }
    public new bool IsFailure => !IsSuccess;
    public T Value { get; }
    private readonly Dictionary<string, List<string>> _errors = new();
    public new IReadOnlyDictionary<string, IEnumerable<string>> Errors
        => _errors.ToDictionary(k => k.Key, v => (IEnumerable<string>)v.Value);

    protected Result(bool isSuccess, T value) : base(isSuccess) => Value = value;

    public static new Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string key, string message)
    {
        var r = new Result<T>(false, default!);
        r._errors[key] = new() { message };
        return r;
    }
    public static new Result<T> Failure(Dictionary<string, IEnumerable<string>> errors)
    {
        var r = new Result<T>(false, default!);
        foreach (var kv in errors) r._errors[kv.Key] = kv.Value.ToList();
        return r;
    }
}
