using Mercadito.Sales.Api.Domain.Products.Entities;

namespace Mercadito.Sales.Api.Application.Products.Models
{
    public class ProductWithCategoriesWriteModel
    {
        public required Product Product { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}
