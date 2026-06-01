using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Auth;

namespace Application.UseCases;

public sealed class ChangePasswordUseCase
{
    private readonly UserService _userService;
    private readonly ILogger<ChangePasswordUseCase> _logger;

    public ChangePasswordUseCase(UserService userService, ILogger<ChangePasswordUseCase> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<Result<bool>> Execute(int userId, ChangePasswordRequest request)
    {
        if (userId <= 0)
        {
            return Result<bool>.Failure(new AppError("AUTH_USER_INVALID", "Usuario autenticado inválido.", ErrorType.Unauthorized));
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Result<bool>.Validation("CurrentPassword", "CURRENT_PASSWORD_REQUIRED", "La contraseña actual es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<bool>.Validation("NewPassword", "NEW_PASSWORD_REQUIRED", "La nueva contraseña es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return Result<bool>.Validation("ConfirmPassword", "CONFIRM_PASSWORD_REQUIRED", "La confirmación de contraseña es obligatoria.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result<bool>.Validation("ConfirmPassword", "CONFIRM_PASSWORD_MISMATCH", "La confirmación no coincide con la nueva contraseña.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return Result<bool>.Validation("NewPassword", "NEW_PASSWORD_SAME_AS_CURRENT", "La nueva contraseña debe ser diferente a la actual.");
        }

        var userOptions = new UserOptions();
        userOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userId);
        userOptions.SelectFields([UserFields.Id, UserFields.Password, UserFields.NeedPasswordChange, UserFields.UpdatedAt]);

        var userResult = await _userService.GetOneAsync(userOptions);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return Result<bool>.Failure(new AppError("USER_NOT_FOUND", "Usuario no encontrado.", ErrorType.NotFound));
        }

        var passwordMatches = PasswordUtils.VerifyPassword(request.CurrentPassword, userResult.Value.Password);
        if (!passwordMatches)
        {
            return Result<bool>.Validation("CurrentPassword", "CURRENT_PASSWORD_INVALID", "La contraseña actual no es correcta.");
        }

        userResult.Value.Password = PasswordUtils.HashPassword(request.NewPassword);
        userResult.Value.NeedPasswordChange = false;
        userResult.Value.UpdatedAt = DateTime.UtcNow;

        var updateOptions = new UserOptions();
        updateOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userId);
        updateOptions.SelectFields([UserFields.Password, UserFields.NeedPasswordChange, UserFields.UpdatedAt]);

        var updateResult = await _userService.UpdateAsync(userResult.Value, updateOptions);
        if (!updateResult.IsSuccess)
        {
            return Result<bool>.Failure(updateResult.Errors);
        }

        _logger.LogInformation("Password changed for user {UserId}.", userId);
        return Result<bool>.Success(true);
    }
}
