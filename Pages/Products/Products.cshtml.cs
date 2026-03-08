using Mercadito.src.categories.domain.model;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.usecases;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Products
{
    public class ProductsModel : PageModel
    {
        private readonly ILogger<ProductsModel> _logger;
        private readonly IProductManagementUseCase _productManagementUseCase;
        private readonly IRegisterNewProductWithCategoryUseCase _registerNewProductWithCategoryUseCase;
        private readonly IUpdateProductUseCase _updateProductUseCase;
        private readonly int _defaultPageSize;

        public List<ProductWithCategoriesModel> Products { get; set; } = [];
        public List<CategoryModel> Categories { get; set; } = [];

        public long CategoryFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public CreateProductDto NewProduct { get; set; } = new CreateProductDto { Name = string.Empty, Description = string.Empty };

        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto { Name = string.Empty, Description = string.Empty };

        [TempData]
        public bool ShowModal { get; set; }

        public ProductsModel(
            ILogger<ProductsModel> logger,
            IProductManagementUseCase productManagementUseCase,
            IRegisterNewProductWithCategoryUseCase registerNewProductWithCategoryUseCase,
            IUpdateProductUseCase updateProductUseCase,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _productManagementUseCase = productManagementUseCase;
            _registerNewProductWithCategoryUseCase = registerNewProductWithCategoryUseCase;
            _updateProductUseCase = updateProductUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public async Task OnGetAsync(long editId = 0)
        {
            var pageParam = Request.Query["page"].ToString();
            var categoryFilterParam = Request.Query["categoryFilter"].ToString();
            await LoadCategoriesAsync();
            await HandleGetRequest(pageParam, categoryFilterParam);
            
            if (editId > 0)
            {
                var editProduct = await _productManagementUseCase.GetForEditAsync(editId);
                if (editProduct != null)
                {
                    EditProduct = editProduct;
                }
            }
        }

        public async Task<IActionResult> OnPostFilterAsync(long categoryFilter = 0)
        {
            CategoryFilter = categoryFilter;
            CurrentPage = 1;
            
            await LoadCategoriesAsync();
            await LoadProductsByState();
            
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewProduct")] CreateProductDto newProduct)
        {
            NewProduct = newProduct;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state != null && state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogError("Campo: {Key} - Error: {ErrorMessage}", key, error.ErrorMessage);
                        }
                    }
                }
                
                ShowModal = true;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }

            try
            {
                await _registerNewProductWithCategoryUseCase.ExecuteAsync(NewProduct);
                
                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToPage();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al crear producto");
                ModelState.AddModelError(string.Empty, validationException.Message);
                ShowModal = true;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear producto");
                ModelState.AddModelError(string.Empty, "Error al guardar el producto. Intente nuevamente.");
                ShowModal = true;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync([Bind(Prefix = "EditProduct")] UpdateProductDto editProduct)
        {
            EditProduct = editProduct;

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                await LoadProductsByState();
                TempData["ShowEditProductModal"] = "true";
                return Page();
            }

            try
            {
                await _updateProductUseCase.ExecuteAsync(EditProduct);
                
                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToPage();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar producto");
                ModelState.AddModelError(string.Empty, validationException.Message);
                await LoadCategoriesAsync();
                await LoadProductsByState();
                TempData["ShowEditProductModal"] = "true";
                return Page();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar producto");
                ModelState.AddModelError(string.Empty, "Error al actualizar el producto. Intente nuevamente.");
                await LoadCategoriesAsync();
                await LoadProductsByState();
                TempData["ShowEditProductModal"] = "true";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                var wasDeleted = await _productManagementUseCase.DeleteAsync(id);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Producto desactivado.";
                }
                else
                {
                    TempData["ErrorMessage"] = "El producto no existe o ya estaba desactivado.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar producto");
                TempData["ErrorMessage"] = "No se pudo eliminar el producto.";
            }
            return RedirectToPage();
        }

        private async Task HandleGetRequest(string pageParam, string categoryFilterParam)
        {
            if (!string.IsNullOrEmpty(pageParam) && string.IsNullOrEmpty(categoryFilterParam))
            {
                await HandlePaginationOnly(pageParam);
            }
            else if (string.IsNullOrEmpty(pageParam) && !string.IsNullOrEmpty(categoryFilterParam))
            {
                await HandleCategoryFilterOnly(categoryFilterParam);
            }
            else if (!string.IsNullOrEmpty(pageParam) && !string.IsNullOrEmpty(categoryFilterParam))
            {
                await HandleCategoryFilterWithPagination(pageParam, categoryFilterParam);
            }
            else
            {
                await HandleDefaultState();
            }
        }

        private async Task HandlePaginationOnly(string pageParam)
        {
            if (int.TryParse(pageParam, out int page) && page > 0)
            {
                CurrentPage = page;
            }
            else
            {
                CurrentPage = 1;
            }
            
            CategoryFilter = 0;
            await LoadAllProductsPaginated();
        }

        private async Task HandleCategoryFilterOnly(string categoryFilterParam)
        {
            CurrentPage = 1;
            CategoryFilter = ResolveCategoryFilter(categoryFilterParam);
            
            if (CategoryFilter != 0)
            {
                await LoadProductsByCategoryPaginated();
            }
            else
            {
                await LoadAllProductsPaginated();
            }
        }

        private async Task HandleCategoryFilterWithPagination(string pageParam, string categoryFilterParam)
        {
            if (int.TryParse(pageParam, out int page) && page > 0)
            {
                CurrentPage = page;
            }
            else
            {
                CurrentPage = 1;
            }
            
            CategoryFilter = ResolveCategoryFilter(categoryFilterParam);
            
            if (CategoryFilter != 0)
            {
                await LoadProductsByCategoryPaginated();
            }
            else
            {
                await LoadAllProductsPaginated();
            }
        }

        private async Task HandleDefaultState()
        {
            CurrentPage = 1;
            CategoryFilter = 0;
            await LoadAllProductsPaginated();
        }

        private long ResolveCategoryFilter(string categoryFilterParam)
        {
            if (string.IsNullOrEmpty(categoryFilterParam))
            {
                return 0;
            }
            
            if (long.TryParse(categoryFilterParam, out long categoryId))
            {
                return categoryId;
            }
            
            var category = Categories.Find(c => c.Code == categoryFilterParam);
            if (category != null)
            {
                return category.Id;
            }
            
            _logger.LogWarning("No se pudo resolver CategoryFilter: {Param}", categoryFilterParam);
            return 0;
        }

        private async Task LoadProductsByState()
        {
            if (CategoryFilter != 0)
            {
                await LoadProductsByCategoryPaginated();
            }
            else
            {
                await LoadAllProductsPaginated();
            }
        }

        private async Task LoadAllProductsPaginated()
        {
            try
            {
                var result = await _productManagementUseCase.GetPageAsync(CurrentPage, 0, _defaultPageSize);
                TotalPages = result.TotalPages;
                Products = [.. result.Products];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar productos");
                Products = [];
                TotalPages = 1;
            }
        }

        private async Task LoadProductsByCategoryPaginated()
        {
            try
            {
                if (CategoryFilter == 0)
                {
                    await LoadAllProductsPaginated();
                    return;
                }

                var result = await _productManagementUseCase.GetPageAsync(CurrentPage, CategoryFilter, _defaultPageSize);
                TotalPages = result.TotalPages;
                Products = [.. result.Products];
                
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar productos filtrados");
                Products = [];
                TotalPages = 1;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = [.. await _productManagementUseCase.GetCategoriesAsync()];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
                Categories = [];
            }
        }
    }
}
