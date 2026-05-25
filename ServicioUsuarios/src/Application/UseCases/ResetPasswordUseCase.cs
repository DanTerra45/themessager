using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Dto.Auth;
using Domain.Entities;
using Domain.Database;
using Domain.Database.Fields;

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

    private async Task<bool> InvalidateToken(PasswordResetToken token)
    {
        var passwordOptions = new PasswordResetTokenOptions();

        passwordOptions.SelectFields([PasswordResetTokenFields.UsedAt]);
        passwordOptions.AddFilter(PasswordResetTokenFields.UserId, FilterOperator.Equals, token.UserId);
        passwordOptions.AddFilter(PasswordResetTokenFields.UsedAt, FilterOperator.IsNull, null);
        token.UsedAt = DateTime.UtcNow;
        var result = await _tokenService.UpdateAsync(token, passwordOptions);

        return result.IsSuccess;
    }
    
    private async Task<Result<PasswordResetToken>> GetValidTokenAsync(string tokenHash)
    {
        var passwordOptions = new PasswordResetTokenOptions();
        passwordOptions.SelectFields([PasswordResetTokenFields.Id,PasswordResetTokenFields.UserId,PasswordResetTokenFields.Token,PasswordResetTokenFields.Expiration,PasswordResetTokenFields.UsedAt,PasswordResetTokenFields.CreatedAt]);
        passwordOptions.AddFilter(PasswordResetTokenFields.Token, FilterOperator.Equals, tokenHash);
        passwordOptions.AddFilter(PasswordResetTokenFields.UsedAt, FilterOperator.IsNull, null);
        passwordOptions.AddFilter(PasswordResetTokenFields.Expiration, FilterOperator.GreaterThan, DateTime.UtcNow);
        var tokenResult = await _tokenService.GetOneAsync(passwordOptions);
        return tokenResult;
    }
    private async Task<bool> MarkTokenAsUsedAsync(PasswordResetToken token)
    {
        var passwordOptions = new PasswordResetTokenOptions();
        passwordOptions.SelectFields([PasswordResetTokenFields.UsedAt]);
        passwordOptions.AddFilter(PasswordResetTokenFields.Id, FilterOperator.Equals, token.Id);
        token.UsedAt = DateTime.UtcNow;
        var result = await _tokenService.UpdateAsync(token, passwordOptions);
        return result.IsSuccess;
    }

    public async Task<Result<bool>> Execute(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<bool>.Failure(new AppError("400", "Token and new password are required.", ErrorType.Validation));
        }

        var tokenHash = PasswordUtils.HashToken(request.Token);
        var tokenResult = await GetValidTokenAsync(tokenHash);
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

        var markTokenResult = await MarkTokenAsUsedAsync(tokenResult.Value);
        if (!markTokenResult)
        {
            return Result<bool>.Failure(new AppError("500", "Failed to mark token as used.", ErrorType.Internal));
        }

        _logger.LogInformation("Password reset completed for user {UserId}", userResult.Value.Id);
        return Result<bool>.Success(true);
    }
}