using ServicioFrontend.Dtos.Common;
using ServicioFrontend.Dtos.Users;

namespace ServicioFrontend.Adapters.Users;

public interface IUsersApiAdapter
{
    Task<ApiResponseDto<IReadOnlyList<UserSummaryDto>>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<ApiResponseDto<IReadOnlyList<AvailableEmployeeDto>>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default);

    Task<ApiResponseDto<int>> RegisterUserAsync(
        RegisterUserRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> SendResetLinkAsync(
        long userId,
        SendPasswordResetLinkRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> AssignTemporaryPasswordAsync(
        long userId,
        AssignTemporaryPasswordRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> DeactivateUserAsync(
        long userId,
        DeactivateUserRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> RequestPasswordResetAsync(
        RequestPasswordResetRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CompletePasswordResetAsync(
        CompletePasswordResetRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> ChangePasswordAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);
}
