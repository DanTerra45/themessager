using Mercadito.src.categories.domain.model;

namespace Mercadito.src.products.domain.usecases
{
    public class ResolveProductPageStateUseCase(ILogger<ResolveProductPageStateUseCase> logger) : IResolveProductPageStateUseCase
    {
        private readonly ILogger<ResolveProductPageStateUseCase> _logger = logger;

        public ProductPageState Resolve(string pageParam, string categoryFilterParam, IReadOnlyList<CategoryModel> categories)
        {
            var currentPage = ResolvePage(pageParam);
            var categoryFilter = ResolveCategoryFilter(categoryFilterParam, categories);

            return new ProductPageState
            {
                CurrentPage = currentPage,
                CategoryFilter = categoryFilter
            };
        }

        private static int ResolvePage(string pageParam)
        {
            if (int.TryParse(pageParam, out int page) && page > 0)
            {
                return page;
            }

            return 1;
        }

        private long ResolveCategoryFilter(string categoryFilterParam, IReadOnlyList<CategoryModel> categories)
        {
            if (string.IsNullOrWhiteSpace(categoryFilterParam))
            {
                return 0;
            }

            if (long.TryParse(categoryFilterParam, out long categoryId))
            {
                return categoryId;
            }

            foreach (var category in categories)
            {
                if (category.Code == categoryFilterParam)
                {
                    return category.Id;
                }
            }

            _logger.LogWarning("No se pudo resolver CategoryFilter: {Param}", categoryFilterParam);
            return 0;
        }
    }
}
