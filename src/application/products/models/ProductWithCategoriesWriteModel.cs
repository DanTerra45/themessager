using Mercadito.src.domain.products.entities;

namespace Mercadito.src.application.products.models
{
    public class ProductWithCategoriesWriteModel
    {
        public required Product Product { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}
