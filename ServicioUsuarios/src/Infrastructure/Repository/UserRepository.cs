using Application.Options;
using Domain.Database;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Dto.Response;
using Dapper;
using Microsoft.Extensions.Logging;
using Infrastructure.Database;
using Domain.Database.Fields;
namespace Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User, int, UserFields, UserOptions, UserSchema>
    {
        private readonly ILogger<UserRepository> _logger;
        public UserRepository(IDbConnectionFactory db, ILogger<BaseRepository<User, int, UserFields, UserOptions, UserSchema>> logger, ILogger<UserRepository> logger2) : base(db, "users", logger)
        {
            _logger = logger2;
        }

        public override async Task<Result<bool>> DeleteAsync(UserOptions? options, CancellationToken cancellationToken = default)
        {
            var userId = options?.Filters.FirstOrDefault(f => f.Field == UserFields.Id)?.Value;
            var (sql, parameters) = new QueryBuilder<UserOptions,UserFields>(_tableName, new UserSchema())
                    .Update<object>(options ?? new UserOptions(), new { State = UserState.Inactive })
                    .Where(options ?? new UserOptions())
                    .Build();
            _logger.LogInformation("Executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
            try
            {
                var affectedRows = await ExecuteNonQueryAsync(sql, parameters);
                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
    }
}