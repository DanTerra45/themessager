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
    public class UserStoryService
    {
        private readonly ICrudRepository<UserStory, int, UserStoryFields, UserStoryOptions> _repository;
        private readonly ILogger<UserStoryService> _logger;
        public UserStoryService(IRepositoryFactory<UserStory,int,UserStoryFields,UserStoryOptions> factory, ILogger<UserStoryService> logger)
        {
            _repository = factory.Create();
            _logger = logger;
        }
        public Task<Result<int>> CreateAsync<TRequest>(TRequest request) where TRequest : class
        {
            if(request is not RegisterUserStoryDto registerRequest)
            {
                _logger.LogWarning("Invalid request type: {Type}", typeof(TRequest).Name);
                return Task.FromResult(Result<int>.Failure(new AppError("400", "Invalid request type", ErrorType.Conflict)));
            }
            var userStory = registerRequest.ToUserStory();
            return _repository.CreateAsync(userStory, null);
        }
    }
}