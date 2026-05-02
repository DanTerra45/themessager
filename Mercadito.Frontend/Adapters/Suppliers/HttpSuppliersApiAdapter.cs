using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Suppliers;

namespace Mercadito.Frontend.Adapters.Suppliers;

public sealed class HttpSuppliersApiAdapter(IHttpClientFactory httpClientFactory) : ISuppliersApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SalesApi");

    public Task<ApiResponseDto<SupplierPageDto>> GetSuppliersAsync(
        string searchTerm,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("searchTerm", searchTerm),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection));

        return GetAsync<SupplierPageDto>($"api/suppliers{query}", cancellationToken);
    }

    public Task<ApiResponseDto<SupplierDto>> GetSupplierAsync(
        long supplierId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<SupplierDto>($"api/suppliers/{supplierId}", cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CreateSupplierAsync(
        SaveSupplierRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveSupplierRequestDto, bool>(
            HttpMethod.Post,
            "api/suppliers",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> UpdateSupplierAsync(
        long supplierId,
        SaveSupplierRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveSupplierRequestDto, bool>(
            HttpMethod.Put,
            $"api/suppliers/{supplierId}",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeleteSupplierAsync(
        long supplierId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Delete,
            $"api/suppliers/{supplierId}",
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
