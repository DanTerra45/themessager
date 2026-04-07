using Mercadito.src.products.domain.entities;
using Mercadito.src.products.application.models;

namespace Mercadito.src.products.domain.factories
{
    public interface IProductFactory
    {
        Product CreateForInsert(CreateProductDto dto);
        Product CreateForUpdate(UpdateProductDto dto);
    }
}

