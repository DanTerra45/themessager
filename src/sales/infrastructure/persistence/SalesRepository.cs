using Mercadito.src.sales.application.ports.output;
using Mercadito.src.shared.domain.exceptions;
using Mercadito.src.shared.infrastructure.persistence;

namespace Mercadito.src.sales.infrastructure.persistence
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
