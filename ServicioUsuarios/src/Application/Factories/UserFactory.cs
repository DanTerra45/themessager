using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Repository;

namespace Application.Factories
{
    public class UserFactory : IRepositoryFactory<User,int,UserFields,UserOptions>
    {
        private readonly UserRepository _repository;
        public UserFactory(UserRepository repository)
        {
            _repository = repository;
        }
        public ICrudRepository<User, int, UserFields, UserOptions> Create()
        {
            return _repository;
        }
    }
}