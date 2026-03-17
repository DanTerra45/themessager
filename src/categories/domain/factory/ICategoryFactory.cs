using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.dto;

namespace Mercadito.src.categories.domain.factory
{
    public interface ICategoryFactory
    {
        Category CreateForInsert(CreateCategoryDto dto);
        Category CreateForUpdate(UpdateCategoryDto dto);
    }
}
