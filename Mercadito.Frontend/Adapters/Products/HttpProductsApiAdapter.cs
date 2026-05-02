using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Products;

namespace Mercadito.Frontend.Adapters.Products;

public sealed class HttpProductsApiAdapter(IHttpClientFactory httpClientFactory) : IProductsApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SalesApi");

    public Task<ApiResponseDto<ProductPageDto>> GetProductsAsync(
        long categoryFilter,
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorProductId,
        long cursorProductId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("categoryFilter", categoryFilter.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection),
            ("anchorProductId", anchorProductId.ToString(CultureInfo.InvariantCulture)),
            ("cursorProductId", cursorProductId.ToString(CultureInfo.InvariantCulture)),
            ("isNextPage", isNextPage.ToString(CultureInfo.InvariantCulture)),
            ("searchTerm", searchTerm));

        return GetAsync<ProductPageDto>($"api/products{query}", cancellationToken);
    }

    public Task<ApiResponseDto<ProductForEditDto>> GetProductAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<ProductForEditDto>($"api/products/{productId}", cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CreateProductAsync(
        SaveProductRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveProductRequestDto, bool>(
            HttpMethod.Post,
            "api/products",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> UpdateProductAsync(
        long productId,
        SaveProductRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveProductRequestDto, bool>(
            HttpMethod.Put,
            $"api/products/{productId}",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeleteProductAsync(
        long productId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Delete,
            $"api/products/{productId}",
            new { },
            actor,
            cancellationToken);
    }

    private async Task<ApiResponseDto<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>(
                cancellationToken: cancellationToken);

            if (apiResponse != null)
            {
                return apiResponse;
            }

            return ApiResponseDto<T>.Fail("El servicio de ventas no devolvió una respuesta válida.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<T>.Fail("No se pudo conectar con el servicio de ventas.");
        }
    }

    private async Task<ApiResponseDto<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string requestUri,
        TRequest request,
        ApiActorContextDto? actor,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(method, requestUri)
            {
                Content = JsonContent.Create(request)
            };

            ActorHeaderWriter.Apply(message, actor);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<TResponse>>(
                cancellationToken: cancellationToken);

            if (apiResponse != null)
            {
                return apiResponse;
            }

            return ApiResponseDto<TResponse>.Fail("El servicio de ventas no devolvió una respuesta válida.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<TResponse>.Fail("No se pudo conectar con el servicio de ventas.");
        }
    }

    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var queryParts = parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
            .Select(parameter => string.Concat(
                Uri.EscapeDataString(parameter.Key),
                "=",
                Uri.EscapeDataString(parameter.Value!)))
            .ToList();

        if (queryParts.Count == 0)
        {
            return string.Empty;
        }

        return string.Concat("?", string.Join("&", queryParts));
    }
}
