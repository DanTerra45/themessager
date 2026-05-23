using Application.Service;
using Application.Utils;
using Application.Options;
using Domain.Common;
using Domain.Entities;
using Domain.Database;
using Domain.Mappers;

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

        var (token, tokenHash) = PasswordUtils.GeneratePasswordResetToken();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        await _tokenService.InvalidateActiveTokensAsync(userId, DateTime.UtcNow);

        var createdToken = await _tokenService.CreateAsync(userId, tokenHash, expiresAtUtc);
        if (!createdToken.IsSuccess)
        {
            return createdToken;
        }

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetAsync(
            userResult.Value.Email,
            userResult.Value.Username,
            resetUrl);

        return Result<bool>.Success(true);
    }
}