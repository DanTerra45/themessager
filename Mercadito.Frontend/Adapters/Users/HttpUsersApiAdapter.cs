using System.Net.Http.Json;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Common;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Users;

namespace Mercadito.Frontend.Adapters.Users;

public sealed class HttpUsersApiAdapter(IHttpClientFactory httpClientFactory) : IUsersApiAdapter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UsersApi");

    public Task<ApiResponseDto<IReadOnlyList<UserSummaryDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<IReadOnlyList<UserSummaryDto>>("api/users/all", cancellationToken);
    }

    public Task<ApiResponseDto<IReadOnlyList<AvailableEmployeeDto>>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<IReadOnlyList<AvailableEmployeeDto>>("api/users/available-employees", cancellationToken);
    }

    public Task<ApiResponseDto<RegisterUserResponseDto>> RegisterUserAsync(
        RegisterUserRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RegisterUserRequestDto, RegisterUserResponseDto>(
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
        return SendAsync<SendPasswordResetLinkRequestDto, bool>(
            HttpMethod.Post,
            $"api/users/{userId}/send-reset-link",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> AssignTemporaryPasswordAsync(
        long userId,
        AssignTemporaryPasswordRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AssignTemporaryPasswordRequestDto, bool>(
            HttpMethod.Post,
            $"api/users/{userId}/temporary-password",
            request,
            actor,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> DeactivateUserAsync(
        long userId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, bool>(
            HttpMethod.Post,
            $"api/users/{userId}/deactivate",
            new { },
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
        return Task.FromResult(ApiResponseDto<bool>.Fail("El servicio de usuarios nuevo todavía no expone el flujo público de restablecimiento."));
    }

    public Task<ApiResponseDto<PasswordResetTokenDto>> ValidatePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponseDto<PasswordResetTokenDto>.Fail("El servicio de usuarios nuevo todavía no expone la validación pública del token."));
    }

    public Task<ApiResponseDto<bool>> CompletePasswordResetAsync(
        CompletePasswordResetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var backendRequest = new
        {
            Token = request.Token,
            NewPassword = request.Password
        };

        return SendAsync<object, bool>(
            HttpMethod.Post,
            "api/auth/reset-password/confirm",
            backendRequest,
            actor: null,
            cancellationToken);
    }

    public Task<ApiResponseDto<bool>> ForcePasswordChangeAsync(
        long userId,
        ForcePasswordChangeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponseDto<bool>.Fail("El servicio de usuarios nuevo todavía no expone el cambio forzado de contraseña para el frontend."));
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

            return ApiResponseDto<T>.Fail("El servicio de usuarios no devolvió una respuesta válida.");
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

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<TResponse>>(
                cancellationToken: cancellationToken);

            if (apiResponse != null)
            {
                return apiResponse;
            }

            return ApiResponseDto<TResponse>.Fail("El servicio de usuarios no devolvió una respuesta válida.");
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

}
