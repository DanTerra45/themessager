using Mercadito.src.products.application.ports.output;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.products.application.models;
using Mercadito.src.shared.domain.repository;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.products.infrastructure.persistence
{
    public partial class ProductRepository(IDbConnectionFactory dbConnection) : IProductRepository, ICrudRepository<ProductWithCategoriesWriteModel, ProductWithCategoriesWriteModel, ProductForEditModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";
        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }
    }
}
