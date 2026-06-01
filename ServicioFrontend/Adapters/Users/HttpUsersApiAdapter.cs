using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using ServicioFrontend.Adapters.Common;
using ServicioFrontend.Authentication;
using ServicioFrontend.Dtos.Common;
using ServicioFrontend.Dtos.Users;

namespace ServicioFrontend.Adapters.Users;

public sealed class HttpUsersApiAdapter(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : IUsersApiAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UsersApi");
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Task<ApiResponseDto<IReadOnlyList<UserSummaryDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<IReadOnlyList<UserSummaryDto>>("api/users/all", cancellationToken);
    }

    public Task<ApiResponseDto<IReadOnlyList<AvailableEmployeeDto>>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default)
    {
        // ServicioUsuarios ya no expone esta consulta.
        return Task.FromResult(new ApiResponseDto<IReadOnlyList<AvailableEmployeeDto>>(true, [], []));
    }

    public Task<ApiResponseDto<int>> RegisterUserAsync(
        RegisterUserRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RegisterUserRequestDto, int>(
            HttpMethod.Post,
            "api/users",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> SendResetLinkAsync(
        long userId,
        SendPasswordResetLinkRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Put,
            $"api/auth/send-reset-password/{userId}",
            new { },
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> AssignTemporaryPasswordAsync(
        long userId,
        AssignTemporaryPasswordRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Patch,
            $"api/users/assign/temporary-password/{userId}",
            new { },
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeactivateUserAsync(
        long userId,
        DeactivateUserRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendDeleteAsync(
            $"api/users/{userId}",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendAsync<LoginRequestDto, LoginResponseDto>(
            HttpMethod.Post,
            "api/auth/login",
            request,
            actor: null,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> RequestPasswordResetAsync(
        RequestPasswordResetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RequestPasswordResetRequestDto, bool>(
            HttpMethod.Put,
            "api/users/forgot-password",
            request,
            actor: null,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> CompletePasswordResetAsync(
        CompletePasswordResetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<CompletePasswordResetRequestDto, bool>(
            HttpMethod.Post,
            "api/auth/reset-password/confirm",
            request,
            actor: null,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> ChangePasswordAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<ChangePasswordRequestDto, bool>(
            HttpMethod.Put,
            "api/auth/change-password",
            request,
            actor: null,
            cancellationToken);
    }

    private async Task<ApiResponseDto<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            ApplyAccessToken(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ParseApiResponseAsync<T>(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<T>.Fail("No se pudo conectar con el servicio de usuarios.");
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
            ApplyAccessToken(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            return await ParseApiResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<TResponse>.Fail("No se pudo conectar con el servicio de usuarios.");
        }
    }

    private async Task<ApiResponseDto<bool>> SendDeleteAsync<TRequest>(
        string requestUri,
        TRequest request,
        ApiActorContextDto? actor,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, requestUri)
            {
                Content = JsonContent.Create(request)
            };

            ActorHeaderWriter.Apply(message, actor);
            ApplyAccessToken(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Compatibilidad: algunos backends de DELETE devuelven texto plano u otro envelope.
                return new ApiResponseDto<bool>(true, true, []);
            }

            return await ParseApiResponseAsync<bool>(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or OperationCanceledException)
        {
            return ApiResponseDto<bool>.Fail("No se pudo conectar con el servicio de usuarios.");
        }
    }

    private void ApplyAccessToken(HttpRequestMessage message)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirstValue(FrontendUserClaimTypes.AccessToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("Cookie", $"access_token={token}");
    }

    private static async Task<ApiResponseDto<T>> ParseApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return response.IsSuccessStatusCode
                ? new ApiResponseDto<T>(true, default, [])
                : ApiResponseDto<T>.Fail($"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase ?? "respuesta vacía"}.");
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var success = root.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.True
                || (successElement.ValueKind == JsonValueKind.False ? false : response.IsSuccessStatusCode);

            T? data = default;
            if (root.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                data = dataElement.Deserialize<T>(JsonOptions);
            }

            var errors = new List<string>();
            var groupedValidation = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in errorsElement.EnumerateArray())
                {
                    switch (item.ValueKind)
                    {
                        case JsonValueKind.String:
                        {
                            var message = item.GetString();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                errors.Add(message);
                            }

                            break;
                        }
                        case JsonValueKind.Object:
                        {
                            var message = item.TryGetProperty("message", out var messageElement)
                                ? messageElement.GetString()
                                : null;
                            var field = item.TryGetProperty("field", out var fieldElement)
                                ? fieldElement.GetString()
                                : null;
                            var type = item.TryGetProperty("type", out var typeElement)
                                ? typeElement.GetString()
                                : null;

                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                errors.Add(message);
                            }

                            if (string.Equals(type, "Validation", StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrWhiteSpace(field)
                                && !string.IsNullOrWhiteSpace(message))
                            {
                                if (!groupedValidation.TryGetValue(field, out var fieldErrors))
                                {
                                    fieldErrors = [];
                                    groupedValidation[field] = fieldErrors;
                                }

                                fieldErrors.Add(message);
                            }

                            break;
                        }
                    }
                }
            }

            if (!success && errors.Count == 0)
            {
                errors.Add($"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase ?? "Error del servicio de usuarios"}.");
            }

            var validation = groupedValidation.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value);

            return new ApiResponseDto<T>(success, data, errors)
            {
                ValidationErrors = validation
            };
        }
        catch (JsonException)
        {
            return response.IsSuccessStatusCode
                ? ApiResponseDto<T>.Fail("El servicio de usuarios no devolvió una respuesta válida.")
                : ApiResponseDto<T>.Fail($"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase ?? "Error del servicio de usuarios"}.");
        }
    }
}
