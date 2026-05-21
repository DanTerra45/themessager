using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Mappers;
using Infrastructure.Repository;

namespace Infrastructure.Service
{
    public class UserService
    {
        private readonly ICrudRepository<User, int, UserFields, UserOptions> _repository;
        private readonly ILogger<UserService> _logger;
        public UserService(IRepositoryFactory<User,int,UserFields,UserOptions> factory, ILogger<UserService> logger)
        {
            _repository = factory.Create();
            _logger = logger;
        }
        public async Task<Result<IEnumerable<UserResponse>>> GetAllAsync(UserOptions? options)
        {
            var result = await _repository.GetAllAsync(options);
            _logger.LogInformation("Fetching all users with options: {@Options}", options);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error occurred while fetching all users.");
                _logger.LogWarning("Errors: {@Errors}", result.Errors);
                _logger.LogInformation("Failed to fetch all users.");
                return Result<IEnumerable<UserResponse>>.Failure(result.Errors);
            }

            _logger.LogInformation("Result count: {Count}", result.Value.Count());
            var mapped = result.Value.Select(u => u.ToResponse());
            return Result<IEnumerable<UserResponse>>.Success(mapped);
        }
    }
}