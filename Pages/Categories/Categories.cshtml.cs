using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.usecases;

namespace Mercadito.Pages.Categories
{
    public class CategoriesModel : PageModel
    {
        private readonly ILogger<CategoriesModel> _logger;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase;
        private readonly int _defaultPageSize;

        public List<CategoryModel> Categories { get; set; } = [];
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public CreateCategoryDto NewCategory { get; set; } = new CreateCategoryDto { Name = string.Empty, Description = string.Empty, Code = string.Empty };

        public UpdateCategoryDto EditCategory { get; set; } = new UpdateCategoryDto { Name = string.Empty, Description = string.Empty, Code = string.Empty };

        public bool ShowEditCategoryModal { get; set; }

        public bool ShowCreateCategoryModal { get; set; }

        public CategoriesModel(
            ILogger<CategoriesModel> logger,
            ICategoryManagementUseCase categoryManagementUseCase,
            IConfiguration configuration)
        {
            _logger = logger;
            _categoryManagementUseCase = categoryManagementUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public async Task OnGetAsync(int pageNumber = 1, long editId = 0)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            await LoadCategoriesAsync();
            if (editId > 0)
            {
                var category = await _categoryManagementUseCase.GetForEditAsync(editId);
                if (category != null)
                {
                    EditCategory = category;
                    ShowEditCategoryModal = true;
                }
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _categoryManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize);
                TotalPages = result.TotalPages;

                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    result = await _categoryManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize);
                }

                Categories = [.. result.Categories];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
                ModelState.AddModelError(string.Empty, "Error al cargar las categorías. Intente nuevamente.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewCategory")] CreateCategoryDto newCategory)
        {
            NewCategory = newCategory;

            if (!ModelState.IsValid)
            {
                ShowCreateCategoryModal = true;
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                await _categoryManagementUseCase.CreateAsync(NewCategory);
                _logger.LogInformation("Categoría creada: {Name}", NewCategory.Name);

                TempData["SuccessMessage"] = "Categoría agregada exitosamente.";
                return RedirectToPage();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Business validation while creating category");
                ModelState.AddModelError(string.Empty, validationException.Message);
                ShowCreateCategoryModal = true;
                await LoadCategoriesAsync();
                return Page();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear categoría");
                ModelState.AddModelError(string.Empty, "Error al guardar la categoría. Intente nuevamente.");
                ShowCreateCategoryModal = true;
                await LoadCategoriesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync([Bind(Prefix = "EditCategory")] UpdateCategoryDto editCategory)
        {
            EditCategory = editCategory;

            _logger.LogInformation("ModelState válido: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Datos recibidos: Id={Id} Code={Code}, Name={Name}, Description={Description}",
                EditCategory.Id, EditCategory.Code, EditCategory.Name, EditCategory.Description);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                ShowEditCategoryModal = true;
                return Page();
            }

            try
            {
                await _categoryManagementUseCase.UpdateAsync(EditCategory);
                _logger.LogInformation("Categoría actualizada: {Id}", EditCategory.Id);
                TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
                return RedirectToPage();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar categoría");
                ModelState.AddModelError(string.Empty, validationException.Message);
                await LoadCategoriesAsync();
                ShowEditCategoryModal = true;
                return Page();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar categoría");
                ModelState.AddModelError(string.Empty, "Error al actualizar la categoría. Intente nuevamente.");
                await LoadCategoriesAsync();
                ShowEditCategoryModal = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                var wasDeleted = await _categoryManagementUseCase.DeleteAsync(id);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Categoría desactivada.";
                }
                else
                {
                    TempData["ErrorMessage"] = "La categoría no existe o ya estaba desactivada.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar la categoría");
                TempData["ErrorMessage"] = "No se pudo eliminar la categoría.";
            }
            return RedirectToPage();
        }

    }
}
