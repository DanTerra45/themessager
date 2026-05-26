using Application.Service;
using Application.Utils;
using Application.Options;
using Domain.Common;
using Domain.Entities;
using Domain.Database;
using Domain.Mappers;
using Domain.Dto.Register;
using Domain.Database.Fields;

namespace Application.UseCases;

public sealed class RequestPasswordResetUseCase
{
    private readonly UserService _userService;
    private readonly PasswordResetTokenService _tokenService;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RequestPasswordResetUseCase> _logger;

    public RequestPasswordResetUseCase(
        UserService userService,
        PasswordResetTokenService tokenService,
        EmailService emailService,
        IConfiguration configuration,
        ILogger<RequestPasswordResetUseCase> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
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
    private async Task<bool> RegisterNewToken(RegisterPasswordResetTokenDto dto)
    {
        var result = await _tokenService.CreateAsync(dto);
        return result.IsSuccess;
    }
    public async Task<PasswordResetToken?> GetActiveTokenAsync(int userId)
    {
        var passwordOptions = new PasswordResetTokenOptions();
        passwordOptions.AddFilter(PasswordResetTokenFields.UserId, FilterOperator.Equals, userId);
        passwordOptions.AddFilter(PasswordResetTokenFields.UsedAt, FilterOperator.IsNull, null);
        var tokenResult = await _tokenService.GetOneAsync(passwordOptions);
        if (tokenResult.IsSuccess && tokenResult.Value is not null)
        {
            if (tokenResult.Value.ExpirationAt < DateTime.UtcNow)
            {
                await InvalidateToken(tokenResult.Value);
                return null;
            }
            return tokenResult.Value;
        }
        return null;
    }
    public async Task<Result<bool>> Execute(int userId)
    {
        var userOptions = new UserOptions();
        userOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userId);

        var userResult = await _userService.GetOneAsync(userOptions);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            _logger.LogWarning("Password reset requested for unknown user {UserId}", userId);
            return Result<bool>.Failure(new AppError("404", "User not found", ErrorType.NotFound));
        }
        var passwordResetTokenOptions = new PasswordResetTokenOptions();
        passwordResetTokenOptions.AddFilter(PasswordResetTokenFields.UserId, FilterOperator.Equals, userId);
        var tokenResult = await this.GetActiveTokenAsync(userId);
        var token = string.Empty;
        if (tokenResult is null)
        {
            token = PasswordUtils.GenerateToken();
            var result = await this.RegisterNewToken(new RegisterPasswordResetTokenDto(
                UserId: userId,
                Token: token
            ));
            if (!result)
            {
                return Result<bool>.Failure(new AppError("500", "Failed to create password reset token", ErrorType.Internal));
            }
        }
        else
        {
            token = PasswordUtils.DecodeToken(tokenResult.TokenHash);
        }
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&username={Uri.EscapeDataString(userResult.Value.Username)}";

        await _emailService.SendPasswordResetAsync(
            userResult.Value.Email,
            userResult.Value.Username,
            resetUrl);

        return Result<bool>.Success(true);
    }
}