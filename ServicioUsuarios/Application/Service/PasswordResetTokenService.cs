using System.Data;
using Application.Options;
using Dapper;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Register;
using Domain.Entities;
using Domain.Factories;

namespace Application.Service;

public sealed class PasswordResetTokenService
{
    private readonly ICrudRepository<PasswordResetToken, int, PasswordResetTokenFields, PasswordResetTokenOptions> _repository;
    private readonly ILogger<PasswordResetTokenService> _logger;

    public PasswordResetTokenService(IRepositoryFactory<PasswordResetToken, int, PasswordResetTokenFields, PasswordResetTokenOptions> factory, ILogger<PasswordResetTokenService> logger)
    {
        _repository = factory.Create();
        _logger = logger;
    }

    public async Task<Result<bool>> UpdateAsync<TRequest>(TRequest request, PasswordResetTokenOptions? options)
    where TRequest : class
    {
        if(request is not PasswordResetToken passwordResetToken)
        {
            _logger.LogWarning("Invalid request type: {Type}", typeof(TRequest).Name);
            return Result<bool>.Failure(new AppError("400", "Invalid request type", ErrorType.Conflict));
        }
        return await _repository.UpdateAsync<TRequest>(request, options);
    }
//     public async Task<Result<bool>> InvalidateActiveTokensAsync()
//     {
//         const string sql = @"
// UPDATE password_reset_token
// SET used_at = @InvalidatedAtUtc
// WHERE user_id = @UserId AND used_at IS NULL";

//         try
//         {
//             var options = new PasswordResetTokenOptions();
//             var result = await _repository.UpdateAsync();
//             return Result<bool>.Success(result);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error invalidating password reset tokens for user {UserId}", userId);
//             return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
//         }
//     }
    public Task<Result<int>> CreateAsync<TRequest>(TRequest request) where TRequest : class
    {
        if(request is not RegisterPasswordResetTokenDto passwordResetToken)
        {
            _logger.LogWarning("Invalid request type: {Type}", typeof(TRequest).Name);
            return Task.FromResult(Result<int>.Failure(new AppError("400", "Invalid request type", ErrorType.Conflict)));
        }
        return _repository.CreateAsync(passwordResetToken, null);
    }
//     public async Task<Result<bool>> CreateAsync(int userId, string tokenHash, DateTime expiresAtUtc)
//     {
//         const string sql = @"
// INSERT INTO password_reset_token (user_id, token_hash, expires_at, created_at)
// VALUES (@UserId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc)";

//         try
//         {
//             using var connection = await _db.CreateConnectionAsync();
//             var result = await connection.ExecuteAsync(sql, new
//             {
//                 UserId = userId,
//                 TokenHash = tokenHash,
//                 ExpiresAtUtc = expiresAtUtc,
//                 CreatedAtUtc = DateTime.UtcNow
//             });

//             return Result<bool>.Success(result > 0);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error creating password reset token for user {UserId}", userId);
//             return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
//         }
//     }

    public async Task<Result<PasswordResetToken>> GetOneAsync(PasswordResetTokenOptions? options)
        {
            return await _repository.GetOneAsync(options);
        }

//     public async Task<Result<PasswordResetToken>> GetValidByTokenHashAsync(string tokenHash, DateTime nowUtc)
//     {
//         const string sql = @"
// SELECT
//     id AS Id,
//     user_id AS UserId,
//     token_hash AS TokenHash,
//     expires_at AS ExpirationAt,
//     used_at AS UsedAt,
//     created_at AS CreatedAt
// FROM password_reset_token
// WHERE token_hash = @TokenHash
//   AND used_at IS NULL
//   AND expires_at > @NowUtc
// LIMIT 1";

//         try
//         {
//             using var connection = await _db.CreateConnectionAsync();
//             var token = await connection.QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { TokenHash = tokenHash, NowUtc = nowUtc });

//             if (token is null)
//             {
//                 return Result<PasswordResetToken>.NotFound("404", "Password reset token not found or expired.");
//             }

//             return Result<PasswordResetToken>.Success(token);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error fetching password reset token");
//             return Result<PasswordResetToken>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
//         }
//     }

//     public async Task<Result<bool>> MarkAsUsedAsync(int id, DateTime usedAtUtc)
//     {
//         const string sql = @"
// UPDATE password_reset_token
// SET used_at = @UsedAtUtc
// WHERE id = @Id";

//         try
//         {
//             using var connection = await _db.CreateConnectionAsync();
//             var result = await connection.ExecuteAsync(sql, new { Id = id, UsedAtUtc = usedAtUtc });
//             return Result<bool>.Success(result > 0);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error marking password reset token {Id} as used", id);
//             return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
//         }
//     }
}