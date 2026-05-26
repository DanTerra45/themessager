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
    public class UserStoryRepository : BaseRepository<UserStory, int, UserStoryFields, UserStoryOptions, UserStorySchema>
    {
        private readonly ILogger<UserStoryRepository> _logger;
        public UserStoryRepository(IDbConnectionFactory db, ILogger<BaseRepository<UserStory, int, UserStoryFields, UserStoryOptions, UserStorySchema>> logger, ILogger<UserStoryRepository> logger2) : base(db, "user_story", logger)
        {
            _logger = logger2;
        }

        public override async Task<Result<bool>> DeleteAsync(UserStoryOptions? options, CancellationToken cancellationToken = default)
        {
            var userId = options?.Filters.FirstOrDefault(f => f.Field == UserStoryFields.Id)?.Value;
            var (sql, parameters) = new QueryBuilder<UserStoryOptions,UserStoryFields>(_tableName, new UserStorySchema())
                    .Update<object>(options ?? new UserStoryOptions(), new { State = UserState.Inactive })
                    .Where(options ?? new UserStoryOptions())
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