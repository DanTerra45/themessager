using Mercadito.src.domain.categories.entities;

namespace Mercadito.src.domain.categories.factories
{
    public sealed record CreateCategoryValues(
        string Code,
        string Name,
        string Description);

    public sealed record UpdateCategoryValues(
        long Id,
        string Code,
        string Name,
        string Description);

    public interface ICategoryFactory
    {
        Category CreateForInsert(CreateCategoryValues input);
        Category CreateForUpdate(UpdateCategoryValues input);
    }
}
