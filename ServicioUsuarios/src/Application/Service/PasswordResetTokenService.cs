using System.Data;
using Dapper;
using Domain.Common;
using Domain.Database;
using Domain.Entities;

namespace Application.Service;

public sealed class PasswordResetTokenService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PasswordResetTokenService> _logger;

    public PasswordResetTokenService(IDbConnectionFactory db, ILogger<PasswordResetTokenService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<bool>> InvalidateActiveTokensAsync(int userId, DateTime invalidatedAtUtc)
    {
        const string sql = @"
UPDATE password_reset_token
SET used_at = @InvalidatedAtUtc
WHERE user_id = @UserId AND used_at IS NULL";

        try
        {
            using var connection = await _db.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, InvalidatedAtUtc = invalidatedAtUtc });
            return Result<bool>.Success(result >= 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating password reset tokens for user {UserId}", userId);
            return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
        }
    }

    public async Task<Result<bool>> CreateAsync(int userId, string tokenHash, DateTime expiresAtUtc)
    {
        const string sql = @"
INSERT INTO password_reset_token (user_id, token_hash, expires_at, created_at)
VALUES (@UserId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc)";

        try
        {
            using var connection = await _db.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow
            });

            return Result<bool>.Success(result > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating password reset token for user {UserId}", userId);
            return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
        }
    }

    public async Task<Result<PasswordResetToken>> GetValidByTokenHashAsync(string tokenHash, DateTime nowUtc)
    {
        const string sql = @"
SELECT
    id AS Id,
    user_id AS UserId,
    token_hash AS TokenHash,
    expires_at AS ExpirationAt,
    used_at AS UsedAt,
    created_at AS CreatedAt
FROM password_reset_token
WHERE token_hash = @TokenHash
  AND used_at IS NULL
  AND expires_at > @NowUtc
LIMIT 1";

        try
        {
            using var connection = await _db.CreateConnectionAsync();
            var token = await connection.QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { TokenHash = tokenHash, NowUtc = nowUtc });

            if (token is null)
            {
                return Result<PasswordResetToken>.NotFound("404", "Password reset token not found or expired.");
            }

            return Result<PasswordResetToken>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching password reset token");
            return Result<PasswordResetToken>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
        }
    }

    public async Task<Result<bool>> MarkAsUsedAsync(int id, DateTime usedAtUtc)
    {
        const string sql = @"
UPDATE password_reset_token
SET used_at = @UsedAtUtc
WHERE id = @Id";

        try
        {
            using var connection = await _db.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(sql, new { Id = id, UsedAtUtc = usedAtUtc });
            return Result<bool>.Success(result > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking password reset token {Id} as used", id);
            return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
        }
    }
}