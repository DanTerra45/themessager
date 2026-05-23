using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Dto.Auth;
using Domain.Entities;
using Domain.Database;

namespace Application.UseCases;

public sealed class ResetPasswordUseCase
{
    private readonly UserService _userService;
    private readonly PasswordResetTokenService _tokenService;
    private readonly ILogger<ResetPasswordUseCase> _logger;

    public ResetPasswordUseCase(
        UserService userService,
        PasswordResetTokenService tokenService,
        ILogger<ResetPasswordUseCase> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<bool>> Execute(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<bool>.Failure(new AppError("400", "Token and new password are required.", ErrorType.Validation));
        }

        var tokenHash = PasswordUtils.HashToken(request.Token);
        var tokenResult = await _tokenService.GetValidByTokenHashAsync(tokenHash, DateTime.UtcNow);
        if (!tokenResult.IsSuccess || tokenResult.Value is null)
        {
            return Result<bool>.Failure(new AppError("401", "Invalid or expired reset token.", ErrorType.Conflict));
        }

        var userOptions = new UserOptions();
        userOptions.AddFilter(UserFields.Id, FilterOperator.Equals, tokenResult.Value.UserId);

        var userResult = await _userService.GetOneAsync(userOptions);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return Result<bool>.Failure(new AppError("404", "User not found.", ErrorType.NotFound));
        }

        userResult.Value.Password = PasswordUtils.HashPassword(request.NewPassword);
        userResult.Value.NeedPasswordChange = false;
        userResult.Value.UpdatedAt = DateTime.UtcNow;

        var updateOptions = new UserOptions();
        updateOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userResult.Value.Id);
        updateOptions.SelectFields([UserFields.Password, UserFields.NeedPasswordChange, UserFields.UpdatedAt]);

        var updateResult = await _userService.UpdateAsync(userResult.Value, updateOptions);
        if (!updateResult.IsSuccess)
        {
            return Result<bool>.Failure(updateResult.Errors);
        }

        var markTokenResult = await _tokenService.MarkAsUsedAsync(tokenResult.Value.Id, DateTime.UtcNow);
        if (!markTokenResult.IsSuccess)
        {
            return Result<bool>.Failure(markTokenResult.Errors);
        }

        _logger.LogInformation("Password reset completed for user {UserId}", userResult.Value.Id);
        return Result<bool>.Success(true);
    }
}