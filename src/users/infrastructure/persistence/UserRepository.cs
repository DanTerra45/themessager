using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.users.infrastructure.persistence
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
