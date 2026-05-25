using Mercadito.Frontend.Dtos.Categories;
using Mercadito.Frontend.Pages.Infrastructure;
using Mercadito.Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Categories;

public sealed class CategoriesModel(
    ICategoriaApiClient categoriaClient,
    IConfiguration configuration,
    ILogger<CategoriesModel> logger) : FrontendPageModel
{
    private const string DefaultSortBy = "name";
    private const string DefaultSortDirection = "asc";

    private readonly int _defaultPageSize = ResolveDefaultPageSize(configuration);

    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }
    public string NextCategoryCodePreview { get; private set; } = "C00001";
    public bool ShowEditCategoryModal { get; private set; }
    public bool ShowCreateCategoryModal { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public long CurrentAnchorCategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = DefaultSortDirection;

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty]
    public CategoryFormDto NewCategory { get; set; } = new();

    [BindProperty]
    public CategoryFormDto EditCategory { get; set; } = new();

    public async Task OnGetAsync()
    {
        NormalizeState();
        await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
    }

    public IActionResult OnPostFilter(string sortBy = "", string sortDirection = "", string searchTerm = "", bool clear = false)
    {
        return RedirectToPage(new
        {
            SortBy = NormalizeSortBy(sortBy),
            SortDirection = NormalizeSortDirection(sortDirection),
            SearchTerm = clear ? string.Empty : NormalizeText(searchTerm),
            CurrentPage = 1,
            CurrentAnchorCategoryId = 0
        });
    }

    public async Task<IActionResult> OnPostNavigateAsync(
        string navigationMode = "",
        long cursorCategoryId = 0,
        string sortBy = "",
        string sortDirection = "",
        int currentPage = 1)
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        CurrentPage = Math.Max(1, currentPage);

        var isNextPage = string.Equals(navigationMode, "next", StringComparison.OrdinalIgnoreCase);
        CurrentPage = isNextPage ? CurrentPage + 1 : Math.Max(1, CurrentPage - 1);

        await LoadCategoriesAsync(useCursor: cursorCategoryId > 0, cursorCategoryId, isNextPage);
        return Page();
    }

    public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
    {
        var normalizedCurrentSortBy = NormalizeSortBy(currentSortBy);
        var normalizedCurrentDirection = NormalizeSortDirection(currentSortDirection);
        var normalizedRequestedSortBy = NormalizeSortBy(sortBy);
        var nextDirection = "asc";

        if (string.Equals(normalizedCurrentSortBy, normalizedRequestedSortBy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedCurrentDirection, "asc", StringComparison.OrdinalIgnoreCase))
        {
            nextDirection = "desc";
        }

        return RedirectToPage(new
        {
            SortBy = normalizedRequestedSortBy,
            SortDirection = nextDirection,
            SearchTerm,
            CurrentPage = 1,
            CurrentAnchorCategoryId = 0
        });
    }

    public async Task<IActionResult> OnPostStartEditAsync(long id, string sortBy = "", string sortDirection = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);

        var result = await categoriaClient.GetCategoryByIdAsync(id);
        if (result != null)
        {
            EditCategory = ToForm(result);
            ShowEditCategoryModal = true;
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo cargar la categoría.";
        }

        await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string sortBy = "", string sortDirection = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        NewCategory.Code = string.IsNullOrWhiteSpace(NewCategory.Code)
            ? NextCategoryCodePreview
            : NewCategory.Code;
        RemoveModelStateForPrefix(nameof(EditCategory));

        if (!IsModelStateValidForPrefix(nameof(NewCategory)))
        {
            LogInvalidModelState(logger, "Categories.Create");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
            ShowCreateCategoryModal = true;
            await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
            return Page();
        }

        var result = await categoriaClient.CreateCategoryAsync(
            ToSaveRequest(NewCategory));

            if (result.Success)
            {
                TempData["Success"] = "Categoría agregada exitosamente.";
                return RedirectToPage(new { SortBy, SortDirection, SearchTerm, CurrentPage, CurrentAnchorCategoryId });
            }

            TempData["Error"] = result.Error ?? "Corrige los errores del formulario.";
            ShowCreateCategoryModal = true;
            await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
            return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(string sortBy = "", string sortDirection = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        RemoveModelStateForPrefix(nameof(NewCategory));

        if (!IsModelStateValidForPrefix(nameof(EditCategory)))
        {
            LogInvalidModelState(logger, "Categories.Edit");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
            ShowEditCategoryModal = true;
            await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
            return Page();
        }

        var result = await categoriaClient.UpdateCategoryAsync(
            EditCategory.Id,
            ToSaveRequest(EditCategory));

            if (result.Success)
            {
                TempData["Success"] = "Categoría actualizada correctamente.";
                return RedirectToPage(new { SortBy, SortDirection, SearchTerm, CurrentPage, CurrentAnchorCategoryId });
            }

            TempData["Error"] = result.Error ?? "Corrige los errores del formulario.";
            ShowEditCategoryModal = true;
            await LoadCategoriesAsync(useCursor: false, cursorCategoryId: 0, isNextPage: true);
            return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, string sortBy = "", string sortDirection = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);

        var result = await categoriaClient.DeleteCategoryAsync(id);

        if (result.Success)
        {
            TempData["Success"] = "Categoría desactivada.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "No se pudo eliminar la categoría.";
        }

        return RedirectToPage(new { SortBy, SortDirection, SearchTerm, CurrentPage, CurrentAnchorCategoryId });
    }

    public string GetSortIcon(string columnName)
    {
        if (!string.Equals(SortBy, NormalizeSortBy(columnName), StringComparison.OrdinalIgnoreCase))
        {
            return "bi-arrow-down-up";
        }

        return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "bi-sort-down"
            : "bi-sort-up";
    }

    private async Task LoadCategoriesAsync(bool useCursor, long cursorCategoryId, bool isNextPage)
    {
        // Call the new categoria client method which doesn't have pagination parameters
        var categoriasResult = await categoriaClient.GetCategoriesAsync();
        
        // Since the new client doesn't support filtering/pagination directly,
        // we'll need to handle that in the frontend or modify the client.
        // For now, let's get all categories and filter/sort/paginate locally
        var categorias = categoriasResult.ToList();
        
        // Apply filtering manually
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var lowerSearchTerm = SearchTerm.ToLowerInvariant();
            categorias = categorias.Where(c => 
                c.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                c.Code.ToLowerInvariant().Contains(lowerSearchTerm) ||
                (c.Description != null && c.Description.ToLowerInvariant().Contains(lowerSearchTerm))
            ).ToList();
        }
        
        // Apply sorting manually
        if (string.Equals(SortBy, "id", StringComparison.OrdinalIgnoreCase))
        {
            if (SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                categorias = categorias.OrderByDescending(c => c.Id).ToList();
            }
            else
            {
                categorias = categorias.OrderBy(c => c.Id).ToList();
            }
        }
        else if (string.Equals(SortBy, "code", StringComparison.OrdinalIgnoreCase))
        {
            if (SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                categorias = categorias.OrderByDescending(c => c.Code).ToList();
            }
            else
            {
                categorias = categorias.OrderBy(c => c.Code).ToList();
            }
        }
        else // default to name
        {
            if (SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                categorias = categorias.OrderByDescending(c => c.Name).ToList();
            }
            else
            {
                categorias = categorias.OrderBy(c => c.Name).ToList();
            }
        }
        
        // Apply pagination manually
        var totalItems = categorias.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)_defaultPageSize);
        var startIndex = (CurrentPage - 1) * _defaultPageSize;
        var endIndex = Math.Min(startIndex + _defaultPageSize, totalItems);
        
        var pagedCategorias = categorias.Skip(startIndex).Take(_defaultPageSize).ToList();
        
        Categories = pagedCategorias;
        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < totalPages;
        NextCategoryCodePreview = "C00001"; // This would need to come from the API in a real implementation
        CurrentAnchorCategoryId = pagedCategorias.Count > 0 ? pagedCategorias[0].Id : 0;
        
        if (string.IsNullOrWhiteSpace(NewCategory.Code))
        {
            NewCategory.Code = NextCategoryCodePreview;
        }
    }

    private static CategoryFormDto ToForm(CategoryDto category)
    {
        return new CategoryFormDto
        {
            Id = category.Id,
            Code = category.Code,
            Name = category.Name,
            Description = category.Description
        };
    }

    private static SaveCategoryRequestDto ToSaveRequest(CategoryFormDto form)
    {
        return new SaveCategoryRequestDto(
            NormalizeText(form.Code).ToUpperInvariant(),
            NormalizeCollapsed(form.Name),
            NormalizeText(form.Description));
    }

    private static string NormalizeSortBy(string sortBy)
    {
        var normalized = NormalizeText(sortBy).ToLowerInvariant();
        return normalized switch
        {
            "id" => "id",
            "code" => "code",
            "productcount" => "productcount",
            _ => DefaultSortBy
        };
    }

    private static string NormalizeSortDirection(string sortDirection)
    {
        return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : DefaultSortDirection;
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string NormalizeCollapsed(string? value)
    {
        return string.Join(' ', NormalizeText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int ResolveDefaultPageSize(IConfiguration configuration)
    {
        return int.TryParse(configuration["Ui:DefaultPageSize"], out var pageSize)
            ? Math.Clamp(pageSize, 5, 50)
            : 10;
    }

    private void NormalizeState()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = NormalizeSortDirection(SortDirection);
        SearchTerm = NormalizeText(SearchTerm);
    }
}