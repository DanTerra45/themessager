namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    IReadOnlyList<string> Errors,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors)
{
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>(true, data, [], EmptyValidationErrors());
    }

    public static ApiResponse<T> Fail(params string[] errors)
    {
        return new ApiResponse<T>(false, default, errors, EmptyValidationErrors());
    }

    public static ApiResponse<T> Fail(IReadOnlyDictionary<string, List<string>> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        var errors = validationErrors
            .SelectMany(error => error.Value)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add("La operación no pudo completarse por errores de validación.");
        }

        var normalizedValidationErrors = validationErrors.ToDictionary(
            error => error.Key,
            error => (IReadOnlyList<string>)error.Value
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList(),
            StringComparer.OrdinalIgnoreCase);

        return new ApiResponse<T>(false, default, errors, normalizedValidationErrors);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyValidationErrors()
    {
        return new Dictionary<string, IReadOnlyList<string>>();
    }
}
