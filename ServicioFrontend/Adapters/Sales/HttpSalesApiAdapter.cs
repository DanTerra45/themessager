using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ServicioFrontend.Adapters.Common;
using ServicioFrontend.Dtos.Common;
using ServicioFrontend.Dtos.Sales;

namespace ServicioFrontend.Adapters.Sales;

public sealed class HttpSalesApiAdapter(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : ISalesApiAdapter
{
    private readonly HttpClient _salesHttpClient = httpClientFactory.CreateClient("SalesApi");
    private readonly HttpClient _reportsHttpClient = httpClientFactory.CreateClient("ReportsApi");
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Task<ApiResponseDto<SalesRegistrationContextDto>> GetRegistrationContextAsync(
        string customerSearchTerm = "",
        string productSearchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("customerSearchTerm", customerSearchTerm),
            ("productSearchTerm", productSearchTerm));

        return GetAsync<SalesRegistrationContextDto>(_salesHttpClient, $"api/sales/context{query}", cancellationToken, "ventas");
    }

    public Task<ApiResponseDto<IReadOnlyList<CustomerOptionDto>>> SearchCustomersAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("searchTerm", searchTerm));
        return GetAsync<IReadOnlyList<CustomerOptionDto>>(_salesHttpClient, $"api/sales/customers{query}", cancellationToken, "ventas");
    }

    public Task<ApiResponseDto<IReadOnlyList<SaleProductOptionDto>>> SearchProductsAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("searchTerm", searchTerm));
        return GetAsync<IReadOnlyList<SaleProductOptionDto>>(_salesHttpClient, $"api/sales/products{query}", cancellationToken, "ventas");
    }

    public Task<ApiResponseDto<IReadOnlyList<SaleSummaryDto>>> GetRecentSalesAsync(
        int take = 20,
        string sortBy = "createdat",
        string sortDirection = "desc",
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string status = "",
        string paymentMethod = "",
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("take", take.ToString(CultureInfo.InvariantCulture)),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection),
            ("fromDate", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("toDate", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("status", status),
            ("paymentMethod", paymentMethod));

        return GetAsync<IReadOnlyList<SaleSummaryDto>>(_reportsHttpClient, $"api/sales/recent{query}", cancellationToken, "reportes");
    }

    public Task<ApiResponseDto<SalesMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<SalesMetricsDto>(_reportsHttpClient, "api/sales/metrics", cancellationToken, "reportes");
    }

    public Task<ApiResponseDto<DailyCashClosingReportDto>> GetDailyCashClosingReportAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("businessDate", businessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        return GetAsync<DailyCashClosingReportDto>(_reportsHttpClient, $"api/sales/reports/daily-cash-closing{query}", cancellationToken, "reportes");
    }

    public Task<ApiResponseDto<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        return GetWithFallbackAsync<SaleDetailDto>(
            _reportsHttpClient,
            _salesHttpClient,
            $"api/sales/{saleId}",
            cancellationToken,
            "reportes",
            "ventas");
    }

    public Task<ApiResponseDto<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        return GetWithFallbackAsync<SaleReceiptDto>(
            _reportsHttpClient,
            _salesHttpClient,
            $"api/sales/{saleId}/receipt",
            cancellationToken,
            "reportes",
            "ventas");
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
            cancellationToken,
            _salesHttpClient,
            "ventas");
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
            cancellationToken,
            _salesHttpClient,
            "ventas");
    }

    private async Task<ApiResponseDto<T>> GetAsync<T>(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken,
        string serviceLabel)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            ApplyAccessToken(request);

            using var response = await client.SendAsync(request, cancellationToken);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>(cancellationToken: cancellationToken);

            if (apiResponse != null)
            {
                return apiResponse;
            }

            return ApiResponseDto<T>.Fail($"El servicio de {serviceLabel} no devolvió una respuesta válida.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<T>.Fail($"No se pudo conectar con el servicio de {serviceLabel}.");
        }
    }

    private async Task<ApiResponseDto<T>> GetWithFallbackAsync<T>(
        HttpClient primaryClient,
        HttpClient fallbackClient,
        string requestUri,
        CancellationToken cancellationToken,
        string primaryLabel,
        string fallbackLabel)
    {
        var primaryResult = await GetAsync<T>(primaryClient, requestUri, cancellationToken, primaryLabel);
        if (primaryResult.Success && primaryResult.Data != null)
        {
            return primaryResult;
        }

        return await GetAsync<T>(fallbackClient, requestUri, cancellationToken, fallbackLabel);
    }

    private async Task<ApiResponseDto<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string requestUri,
        TRequest request,
        ApiActorContextDto? actor,
        CancellationToken cancellationToken,
        HttpClient client,
        string serviceLabel)
    {
        try
        {
            using var message = new HttpRequestMessage(method, requestUri)
            {
                Content = JsonContent.Create(request)
            };

            ApplyAccessToken(message);
            ActorHeaderWriter.Apply(message, actor);

            using var response = await client.SendAsync(message, cancellationToken);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<TResponse>>(cancellationToken: cancellationToken);

            if (apiResponse != null)
            {
                return apiResponse;
            }

            return ApiResponseDto<TResponse>.Fail($"El servicio de {serviceLabel} no devolvió una respuesta válida.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<TResponse>.Fail($"No se pudo conectar con el servicio de {serviceLabel}.");
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

    private void ApplyAccessToken(HttpRequestMessage message) =>
        AccessTokenHeaderWriter.Apply(message, _httpContextAccessor);
}
