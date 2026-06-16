namespace ServicioVentas.Contracts.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    IReadOnlyList<string> Errors,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors)
{
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, [], EmptyValidationErrors());

    public static ApiResponse<T> Fail(params string[] errors) =>
        new(false, default, errors, EmptyValidationErrors());

    public static ApiResponse<T> Fail(IReadOnlyDictionary<string, IReadOnlyList<string>> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        var errors = validationErrors
            .SelectMany(pair => pair.Value)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add("La operación no pudo completarse por errores de validación.");
        }

        return new ApiResponse<T>(
            false,
            default,
            errors,
            validationErrors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.ToList(),
                StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyValidationErrors() =>
        new Dictionary<string, IReadOnlyList<string>>();
}
