namespace Mercadito.Frontend.Services;

using Mercadito.Frontend.Dtos.Categories;

public interface ICategoriaApiClient
{
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(long id);
    Task<(bool Success, string? Error)> CreateCategoryAsync(SaveCategoryRequestDto request);
    Task<(bool Success, string? Error)> UpdateCategoryAsync(long id, SaveCategoryRequestDto request);
    Task<(bool Success, string? Error)> DeleteCategoryAsync(long id);
}