using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Mappers;
using Infrastructure.Repository;

namespace Application.Service
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
        public Task<Result<int>> CreateAsync<TRequest>(TRequest request) where TRequest : class
        {
            if(request is not RegisterUserDto registerRequest)
            {
                _logger.LogWarning("Invalid request type: {Type}", typeof(TRequest).Name);
                return Task.FromResult(Result<int>.Failure(new AppError("400", "Invalid request type", ErrorType.Conflict)));
            }
            var user = registerRequest.ToEntity();
            return _repository.CreateAsync(user, null);
        }
        public async Task<Result<User>> GetOneAsync(UserOptions? options)
        {
            return await _repository.GetOneAsync(options);
        }
        public async Task<Result<bool>> UpdateAsync<TRequest>(TRequest request, UserOptions? options) where TRequest : class
        {
            if(request is not User user)
            {
                _logger.LogWarning("Invalid request type: {Type}", typeof(TRequest).Name);
                return Result<bool>.Failure(new AppError("400", "Invalid request type", ErrorType.Conflict));
            }
            return await _repository.UpdateAsync<TRequest>(request, options);
        }
        public async Task<Result<bool>> DeleteAsync(UserOptions? options)
        {
            return await _repository.DeleteAsync(options);
        }
    }
}