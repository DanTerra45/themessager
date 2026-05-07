using Mercadito.Sales.Api.Domain.Categories.Entities;

namespace Mercadito.Sales.Api.Domain.Categories.Factories
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
