using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Common;
using ServicioVentas.Contracts.Sales;

namespace Infrastructure.Integrations;

public sealed class CatalogApiClient(HttpClient httpClient, ILogger<CatalogApiClient> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<IReadOnlyList<SaleProductOptionResponse>>> SearchProductsAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/products?pageSize=25&sortBy=name&sortDirection=asc&searchTerm={Uri.EscapeDataString(searchTerm ?? string.Empty)}";
        var responseResult = await GetAsync<CatalogProductPageResponse>(query, cancellationToken);
        if (responseResult.IsFailure || responseResult.Value is null)
        {
            return Result<IReadOnlyList<SaleProductOptionResponse>>.Failure(responseResult.Errors);
        }

        var products = responseResult.Value.Products
            .Select(product => new SaleProductOptionResponse(
                product.Id,
                product.Name,
                product.Batch,
                product.Stock,
                product.Price))
            .ToList();

        return Result<IReadOnlyList<SaleProductOptionResponse>>.Success(products);
    }

    public Task<Result<CatalogProductResponse>> GetProductByIdAsync(
        long productId,
        CancellationToken cancellationToken = default) =>
        GetAsync<CatalogProductResponse>($"api/products/{productId.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

    public Task<Result<bool>> ReserveStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default) =>
        PostStockAsync($"api/products/{productId.ToString(CultureInfo.InvariantCulture)}/stock/reserve", quantity, actorUserId, actorUsername, cancellationToken);

    public Task<Result<bool>> RecoverStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default) =>
        PostStockAsync($"api/products/{productId.ToString(CultureInfo.InvariantCulture)}/stock/recover", quantity, actorUserId, actorUsername, cancellationToken);

    private async Task<Result<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            var payload = await ReadApiResponseAsync<T>(response, cancellationToken);
            if (payload.Success && payload.Data is not null)
            {
                return Result<T>.Success(payload.Data);
            }

            return Result<T>.Failure(ToErrors(payload.Errors));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogError(exception, "No se pudo consultar el catálogo en {RequestUri}.", requestUri);
            return Result<T>.Internal("CatalogUnavailable", "No se pudo conectar con el servicio de catálogo.");
        }
    }

    private async Task<Result<bool>> PostStockAsync(
        string requestUri,
        int quantity,
        long actorUserId,
        string actorUsername,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(new { quantity })
            };

            request.Headers.TryAddWithoutValidation("X-User-Id", actorUserId.ToString(CultureInfo.InvariantCulture));
            request.Headers.TryAddWithoutValidation("X-Username", actorUsername);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await ReadApiResponseAsync<bool>(response, cancellationToken);
            if (payload.Success)
            {
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(ToErrors(payload.Errors));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogError(exception, "No se pudo ejecutar la operación de stock del catálogo en {RequestUri}.", requestUri);
            return Result<bool>.Internal("CatalogUnavailable", "No se pudo conectar con el servicio de catálogo.");
        }
    }

    private static async Task<CatalogApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadFromJsonAsync<CatalogApiResponse<T>>(SerializerOptions, cancellationToken);
        if (payload is not null)
        {
            return payload;
        }

        return new CatalogApiResponse<T>(false, default, ["El servicio de catálogo no devolvió una respuesta válida."], EmptyValidationErrors());
    }

    private static IReadOnlyCollection<AppError> ToErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return [new AppError("CatalogError", "El servicio de catálogo devolvió un error no especificado.", ErrorType.Internal)];
        }

        return errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => new AppError("CatalogError", error, ErrorType.Internal))
            .ToList();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyValidationErrors() =>
        new Dictionary<string, IReadOnlyList<string>>();

    private sealed record CatalogApiResponse<T>(
        bool Success,
        T? Data,
        IReadOnlyList<string> Errors,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors);

    private sealed record CatalogProductPageResponse(
        IReadOnlyList<CatalogProductSummary> Products,
        IReadOnlyList<object> Categories,
        bool HasPreviousPage,
        bool HasNextPage);

    private sealed record CatalogProductSummary(
        long Id,
        string Name,
        string Description,
        int Stock,
        string Batch,
        DateOnly ExpirationDate,
        decimal Price,
        IReadOnlyList<string> Categories);

    public sealed record CatalogProductResponse(
        long Id,
        string Name,
        string Description,
        int Stock,
        string Batch,
        DateOnly ExpirationDate,
        decimal Price,
        IReadOnlyList<long> CategoryIds);
}
