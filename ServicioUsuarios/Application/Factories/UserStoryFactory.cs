using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Repository;

namespace Application.Factories
{
    public class UserStoryFactory : IRepositoryFactory<UserStory,int,UserStoryFields,UserStoryOptions>
    {
        private readonly UserStoryRepository _repository;
        public UserStoryFactory(UserStoryRepository repository)
        {
            _repository = repository;
        }
        public ICrudRepository<UserStory, int, UserStoryFields, UserStoryOptions> Create()
        {
            return _repository;
        }
    }
}