using Mercadito.src.products.data.entity;

namespace Mercadito.src.products.domain.model
{
    public class ProductWithCategoriesWriteModel
    {
        public required Product Product { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}
