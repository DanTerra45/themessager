using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Users;

namespace Mercadito.Frontend.Adapters.Users;

public interface IUsersApiAdapter
{
    Task<ApiResponseDto<IReadOnlyList<UserSummaryDto>>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<ApiResponseDto<IReadOnlyList<AvailableEmployeeDto>>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default);

    Task<ApiResponseDto<RegisterUserResponseDto>> RegisterUserAsync(
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
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> RequestPasswordResetAsync(
        RequestPasswordResetRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<PasswordResetTokenDto>> ValidatePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CompletePasswordResetAsync(
        CompletePasswordResetRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> ForcePasswordChangeAsync(
        long userId,
        ForcePasswordChangeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
