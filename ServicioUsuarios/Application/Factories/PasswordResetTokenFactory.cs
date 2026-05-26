using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Repository;

namespace Application.Factories
{
    public class PasswordResetTokenFactory : IRepositoryFactory<PasswordResetToken,int,PasswordResetTokenFields,PasswordResetTokenOptions>
    {
        private readonly PasswordResetTokenRepository _repository;
        public PasswordResetTokenFactory(PasswordResetTokenRepository repository)
        {
            _repository = repository;
        }
        public ICrudRepository<PasswordResetToken, int, PasswordResetTokenFields, PasswordResetTokenOptions> Create()
        {
            return _repository;
        }
    }
}