using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Categories;
using Mercadito.Frontend.Dtos.Common;

namespace Mercadito.Frontend.Adapters.Categories;

public sealed class HttpCategoriesApiAdapter(IHttpClientFactory httpClientFactory) : ICategoriesApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SalesApi");

    public Task<ApiResponseDto<CategoryPageDto>> GetCategoriesAsync(
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorCategoryId,
        long cursorCategoryId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("pageSize", pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection),
            ("anchorCategoryId", anchorCategoryId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("cursorCategoryId", cursorCategoryId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("isNextPage", isNextPage.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("searchTerm", searchTerm));

        return GetAsync<CategoryPageDto>($"api/categories{query}", cancellationToken);
    }

    public Task<ApiResponseDto<CategoryDto>> GetCategoryAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<CategoryDto>($"api/categories/{categoryId}", cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CreateCategoryAsync(
        SaveCategoryRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveCategoryRequestDto, bool>(
            HttpMethod.Post,
            "api/categories",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> UpdateCategoryAsync(
        long categoryId,
        SaveCategoryRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveCategoryRequestDto, bool>(
            HttpMethod.Put,
            $"api/categories/{categoryId}",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeleteCategoryAsync(
        long categoryId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Delete,
            $"api/categories/{categoryId}",
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
