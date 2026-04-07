using Mercadito.src.products.domain.entities;

namespace Mercadito.src.products.application.models
{
    public class ProductWithCategoriesWriteModel
    {
        public required Product Product { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}

