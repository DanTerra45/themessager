using Mercadito.src.categories.domain.model;

namespace Mercadito.src.products.domain.usecases
{
    public sealed class ProductPageState
    {
        public int CurrentPage { get; init; }
        public long CategoryFilter { get; init; }
    }

    public interface IResolveProductPageStateUseCase
    {
        ProductPageState Resolve(string pageParam, string categoryFilterParam, IReadOnlyList<CategoryModel> categories);
    }
}
