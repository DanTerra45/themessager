using Mercadito.src.categories.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.categories.domain.factory
{
    public class CategoryRepositoryCreator(CategoryRepository categoryRepository)
        : RepositoryCreator<CategoryRepository>
    {
        private readonly CategoryRepository _categoryRepository = categoryRepository;

        public override CategoryRepository Create()
        {
            return _categoryRepository;
        }
    }
}
