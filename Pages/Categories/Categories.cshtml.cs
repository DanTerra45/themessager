using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito
{
    public class CategoriesModel : PageModel
    {
        private readonly ILogger<CategoriesModel> _logger;
        private readonly ICategoryRepository _categoryRepository;
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        [BindProperty]
        public CreateCategoryDto NewCategory { get; set; } = new CreateCategoryDto();

        public CategoriesModel(ILogger<CategoriesModel> logger, ICategoryRepository categoryRepository)
        {
            _logger = logger;
            _categoryRepository = categoryRepository;
        }
        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
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
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                await _categoryRepository.AddCategoryAsync(NewCategory);
                _logger.LogInformation("Categoría creada: {Name}", NewCategory.Name);
                
                TempData["SuccessMessage"] = "Categoría agregada exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                ModelState.AddModelError(string.Empty, "Error al guardar la categoría. Intente nuevamente.");
                await LoadCategoriesAsync();
                return Page();
            }
        }
    }
}
