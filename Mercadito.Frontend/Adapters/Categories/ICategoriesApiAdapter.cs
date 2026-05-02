using Mercadito.Frontend.Dtos.Categories;
using Mercadito.Frontend.Dtos.Common;

namespace Mercadito.Frontend.Adapters.Categories;

public interface ICategoriesApiAdapter
{
    Task<ApiResponseDto<CategoryPageDto>> GetCategoriesAsync(
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorCategoryId,
        long cursorCategoryId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<CategoryDto>> GetCategoryAsync(
        long categoryId,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CreateCategoryAsync(
        SaveCategoryRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> UpdateCategoryAsync(
        long categoryId,
        SaveCategoryRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> DeleteCategoryAsync(
        long categoryId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
