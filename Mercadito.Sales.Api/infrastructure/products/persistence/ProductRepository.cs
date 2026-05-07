using Mercadito.Sales.Api.Application.Products.Ports.Output;
using Mercadito.Sales.Api.Infrastructure.Shared.Persistence;
using Mercadito.Sales.Api.Application.Products.Models;
using Mercadito.Sales.Api.Domain.Shared.Repository;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;

namespace Mercadito.Sales.Api.Infrastructure.Products.Persistence
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
