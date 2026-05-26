using ServicioCatalogo.Application.Products.Ports.Output;
using ServicioCatalogo.Infrastructure.Shared.Persistence;
using ServicioCatalogo.Application.Products.Models;
using ServicioCatalogo.Domain.Shared.Repository;
using ServicioCatalogo.Domain.Shared.Exceptions;

namespace ServicioCatalogo.Infrastructure.Products.Persistence
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

