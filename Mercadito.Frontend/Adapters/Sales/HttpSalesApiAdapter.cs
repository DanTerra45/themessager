using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Sales;

namespace Mercadito.Frontend.Adapters.Sales;

public sealed class HttpSalesApiAdapter(IHttpClientFactory httpClientFactory) : ISalesApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SalesApi");

    public Task<ApiResponseDto<SalesRegistrationContextDto>> GetRegistrationContextAsync(
        string customerSearchTerm = "",
        string productSearchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("customerSearchTerm", customerSearchTerm),
            ("productSearchTerm", productSearchTerm));

        return GetAsync<SalesRegistrationContextDto>($"api/sales/context{query}", cancellationToken);
    }

    public Task<ApiResponseDto<IReadOnlyList<CustomerOptionDto>>> SearchCustomersAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("searchTerm", searchTerm));
        return GetAsync<IReadOnlyList<CustomerOptionDto>>($"api/sales/customers{query}", cancellationToken);
    }

    public Task<ApiResponseDto<IReadOnlyList<SaleProductOptionDto>>> SearchProductsAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("searchTerm", searchTerm));
        return GetAsync<IReadOnlyList<SaleProductOptionDto>>($"api/sales/products{query}", cancellationToken);
    }

    public Task<ApiResponseDto<IReadOnlyList<SaleSummaryDto>>> GetRecentSalesAsync(
        int take = 20,
        string sortBy = "createdat",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("take", take.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection));

        return GetAsync<IReadOnlyList<SaleSummaryDto>>($"api/sales/recent{query}", cancellationToken);
    }

    public Task<ApiResponseDto<SalesMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<SalesMetricsDto>("api/sales/metrics", cancellationToken);
    }

    public Task<ApiResponseDto<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        return GetAsync<SaleDetailDto>($"api/sales/{saleId}", cancellationToken);
    }

    public Task<ApiResponseDto<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        return GetAsync<SaleReceiptDto>($"api/sales/{saleId}/receipt", cancellationToken);
    }

    public Task<ApiResponseDto<SaleReceiptDto>> RegisterSaleAsync(
        RegisterSaleRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RegisterSaleRequestDto, SaleReceiptDto>(
            HttpMethod.Post,
            "api/sales",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CancelSaleAsync(
        long saleId,
        string reason,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<CancelSaleRequestDto, bool>(
            HttpMethod.Post,
            $"api/sales/{saleId}/cancel",
            new CancelSaleRequestDto(saleId, reason),
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
