using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.dto;

namespace Mercadito.src.products.domain.factory
{
    public interface IProductFactory
    {
        Product CreateForInsert(CreateProductDto dto);
        Product CreateForUpdate(UpdateProductDto dto);
    }
}
