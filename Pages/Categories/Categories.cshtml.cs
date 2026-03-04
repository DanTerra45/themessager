using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    public class CategoriesModel : PageModel
    {
        private readonly ILogger<CategoriesModel> _logger;
        private readonly ICategoryRepository _categoryRepository;
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public CreateCategoryDto NewCategory { get; set; } = new CreateCategoryDto();
        public UpdateCategoryDto EditCategory { get; set; } = new UpdateCategoryDto();

        public CategoriesModel(ILogger<CategoriesModel> logger, ICategoryRepository categoryRepository)
        {
            _logger = logger;
            _categoryRepository = categoryRepository;
        }

        public async Task OnGetAsync(Guid? editId)
        {
            await LoadCategoriesAsync();
            if (editId.HasValue)
            {
                var category = await _categoryRepository.GetCategoryByIdAsync(editId.Value);
                if (category != null)
                {
                    EditCategory = new UpdateCategoryDto
                    {
                        Id = category.Id,
                        Code = category.Code,
                        Name = category.Name,
                        Description = category.Description
                    };
                }
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var resultado = (await _categoryRepository.GetAllCategoriesAsync()).ToList();
                TotalPages = (int)Math.Ceiling(resultado.Count / 10.0);
                resultado = await _categoryRepository.GetCategoryByPages(CurrentPage) as List<CategoryModel>;
                Categories = resultado ?? new List<CategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categorías");
                ModelState.AddModelError(string.Empty, "Error al cargar las categorías. Intente nuevamente.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(CreateCategoryDto newCategory)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                NewCategory = newCategory; // Conservar datos si falla
                return Page();
            }

            try
            {
                await _categoryRepository.AddCategoryAsync(newCategory);
                _logger.LogInformation("Categoría creada: {Name}", newCategory.Name);

                TempData["SuccessMessage"] = "Categoría agregada exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                ModelState.AddModelError(string.Empty, "Error al guardar la categoría. Intente nuevamente.");
                await LoadCategoriesAsync();
                NewCategory = newCategory;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(UpdateCategoryDto editCategory)
        {
            _logger.LogInformation("ModelState válido: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Datos recibidos: Id={Id} Code={Code}, Name={Name}, Description={Description}",
                editCategory.Id, editCategory.Code, editCategory.Name, editCategory.Description);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                EditCategory = editCategory; // Conservar datos si falla
                return RedirectToPage(new { editId = editCategory.Id });
            }

            try
            {
                var existing = await _categoryRepository.GetCategoryByIdAsync(editCategory.Id);
                if (existing == null)
                {
                    ModelState.AddModelError(string.Empty, "Categoría no encontrada.");
                    await LoadCategoriesAsync();
                    EditCategory = editCategory;
                    return RedirectToPage(new { editId = editCategory.Id });
                }

                var updatedCategory = new Category
                {
                    Id = editCategory.Id,
                    Code = editCategory.Code,
                    Name = editCategory.Name,
                    Description = editCategory.Description ?? string.Empty
                };

                await _categoryRepository.UpdateCategoryAsync(updatedCategory);
                _logger.LogInformation("Categoría actualizada: {Id}", editCategory.Id);
                TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría");
                ModelState.AddModelError(string.Empty, "Error al actualizar la categoría. Intente nuevamente.");
                await LoadCategoriesAsync();
                EditCategory = editCategory;
                return RedirectToPage(new { editId = editCategory.Id });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _categoryRepository.DeleteCategoryAsync(id);
                TempData["SuccessMessage"] = "Categoría eliminada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la categoría");
                TempData["ErrorMessage"] = "No se pudo eliminar la categoría.";
            }
            return RedirectToPage();
        }
    }
}
