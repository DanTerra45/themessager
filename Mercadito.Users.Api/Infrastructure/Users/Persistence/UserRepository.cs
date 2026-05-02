using Mercadito.Users.Api.Infrastructure.Shared.Persistence;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Domain.Shared.Exceptions;

namespace Mercadito.Users.Api.Infrastructure.Users.Persistence
{
    public sealed partial class UserRepository(IDbConnectionFactory dbConnectionFactory) : IUserRepository, IUserAccessWorkflowRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory = dbConnectionFactory;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }
    }
}
