using System;

namespace Mercadito
{
    public class RegisterCategoryUseCase
    {
        private readonly ICategoryRepository _categoryRepository;

        public RegisterCategoryUseCase(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async void Execute(CreateCategoryDto newCategory)
        {
            await _categoryRepository.AddCategoryAsync(newCategory);
        }
    }
}