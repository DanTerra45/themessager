using Mercadito.Sales.Api.Application.Sales.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;
using Mercadito.Sales.Api.Infrastructure.Shared.Persistence;

namespace Mercadito.Sales.Api.Infrastructure.Sales.Persistence
{
    public sealed partial class SalesRepository(IDbConnectionFactory dbConnection) : ISalesRepository
    {
        private const int SaleCodeSequenceId = 1;
        private const int MaximumSaleCodeNumber = 99999;
        private const string RegisteredStatus = "Registrada";
        private const string CancelledStatus = "Anulada";
        private const string ActiveState = "A";
        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }
    }
}
