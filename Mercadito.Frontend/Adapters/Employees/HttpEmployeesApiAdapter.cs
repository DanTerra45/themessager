using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Employees;

namespace Mercadito.Frontend.Adapters.Employees;

public sealed class HttpEmployeesApiAdapter(IHttpClientFactory httpClientFactory) : IEmployeesApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SalesApi");

    public Task<ApiResponseDto<EmployeePageDto>> GetEmployeesAsync(
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorEmployeeId,
        long cursorEmployeeId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("pageSize", pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection),
            ("anchorEmployeeId", anchorEmployeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("cursorEmployeeId", cursorEmployeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("isNextPage", isNextPage ? "true" : "false"),
            ("searchTerm", searchTerm));

        return GetAsync<EmployeePageDto>($"api/employees{query}", cancellationToken);
    }

    public Task<ApiResponseDto<EmployeeDto>> GetEmployeeAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<EmployeeDto>($"api/employees/{employeeId}", cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CreateEmployeeAsync(
        SaveEmployeeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveEmployeeRequestDto, bool>(
            HttpMethod.Post,
            "api/employees",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> UpdateEmployeeAsync(
        long employeeId,
        SaveEmployeeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SaveEmployeeRequestDto, bool>(
            HttpMethod.Put,
            $"api/employees/{employeeId}",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeleteEmployeeAsync(
        long employeeId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Delete,
            $"api/employees/{employeeId}",
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
