namespace Mercadito.Frontend.Dtos.Common;

public sealed record ApiResponseDto<T>(
    bool Success,
    T? Data,
    IReadOnlyList<string> Errors)
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    public static ApiResponseDto<T> Fail(string error)
    {
        return new ApiResponseDto<T>(false, default, [error]);
    }
}
