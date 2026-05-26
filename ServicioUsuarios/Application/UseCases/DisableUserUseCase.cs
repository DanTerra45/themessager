using Application.Options;
using Application.Service;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Register;
using Domain.Entities;
using Domain.Mappers;
using Dapper;
using Microsoft.Extensions.Logging;
using Infrastructure.Database;
using Domain.Entities.Enums;

namespace Application.UseCases
{
    public class DisableUserUseCase
    {
        private readonly UserStoryService _userStoryService;
        private readonly UserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DisableUserUseCase> _logger;

        public DisableUserUseCase(UserStoryService userStoryService, UserService userService, IUnitOfWork unitOfWork, ILogger<DisableUserUseCase> logger)
        {
            _userStoryService = userStoryService;
            _userService = userService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        private async Task<Result<bool>> CheckUserRoleAsync(int userId)
        {
            var options = new UserOptions();
            options.SelectFields([UserFields.Role]);
            options.AddFilter(UserFields.Id, FilterOperator.Equals, userId);
            var result = await _userService.GetOneAsync(options);
            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Errors);
            }
            var user = result.Value;
            if (user == null)
            {
                return Result<bool>.Failure(new AppError("NotFound", "User not found", ErrorType.NotFound));
            }
            if (user.Role == UserRole.Admin)
            {
                return Result<bool>.Failure(new AppError("Forbidden", "Cannot disable an admin user", ErrorType.Forbidden));
            }
            return Result<bool>.Success(true);
        }
        private async Task<Result<bool>> UpdateUserStateAsync(RegisterUserStoryDto request)
        {
            var user = new User
            {
                Id = request.UserId,
                State = request.NewState ?? UserState.Inactive
            };
            var options = new UserOptions();
            options.SelectFields([UserFields.State]);
            options.AddFilter(UserFields.Id, FilterOperator.Equals, request.UserId);

            var builder = new QueryBuilder<UserOptions, UserFields>("users", new UserSchema());
            var (sql, parameters) = builder.Update<User>(options, user).Where(options).Build();

            _logger.LogInformation("Executing update (transaction): {Sql} with params {@P}", sql, parameters);
            var affected = await _unitOfWork.Connection.ExecuteAsync(sql, parameters, _unitOfWork.Transaction);
            return affected > 0 ? Result<bool>.Success(true) : Result<bool>.Failure(new AppError("NotUpdated", "No rows updated", ErrorType.Conflict));
        }

        public async Task<Result<int>> ExecuteAsync(RegisterUserStoryDto request)
        {
            try
            {
                await _unitOfWork.BeginAsync();

                var updateResult = await UpdateUserStateAsync(request);
                if (!updateResult.IsSuccess)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<int>.Failure(updateResult.Errors);
                }

                var story = request.ToUserStory();
                var storyBuilder = new QueryBuilder<UserStoryOptions, UserStoryFields>("user_story", new UserStorySchema());
                var (insertSql, insertParams) = storyBuilder.Insert<UserStory>(new UserStoryOptions(), story).Build();
                var insertReturning = insertSql + " RETURNING id";
                _logger.LogInformation("Inserting user story (transaction): {Sql}", insertReturning);
                var newId = await _unitOfWork.Connection.QuerySingleAsync<int>(insertReturning, insertParams, _unitOfWork.Transaction);

                await _unitOfWork.CommitAsync();
                return Result<int>.Success(newId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling user, rolling back.");
                try { await _unitOfWork.RollbackAsync(); } catch { }
                return Result<int>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
    }   
}
