using Mercadito.src.categories.domain.entities;
using Mercadito.src.categories.application.models;

namespace Mercadito.src.categories.domain.factories
{
    public interface ICategoryFactory
    {
        Category CreateForInsert(CreateCategoryDto dto);
        Category CreateForUpdate(UpdateCategoryDto dto);
    }
}

