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
        private readonly IResolveProductPageStateUseCase _resolveProductPageStateUseCase;
        private readonly int _defaultPageSize;

        public List<ProductWithCategoriesModel> Products { get; set; } = [];
        public List<CategoryModel> Categories { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public long CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public CreateProductDto NewProduct { get; set; } = new CreateProductDto
        {
            Name = string.Empty,
            Description = string.Empty,
            Batch = DateOnly.FromDateTime(DateTime.Today),
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3))
        };

        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto { Name = string.Empty, Description = string.Empty };

        [TempData]
        public bool ShowModal { get; set; }

        public ProductsModel(
            ILogger<ProductsModel> logger,
            IProductManagementUseCase productManagementUseCase,
            IRegisterNewProductWithCategoryUseCase registerNewProductWithCategoryUseCase,
            IUpdateProductUseCase updateProductUseCase,
            IResolveProductPageStateUseCase resolveProductPageStateUseCase,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _productManagementUseCase = productManagementUseCase;
            _registerNewProductWithCategoryUseCase = registerNewProductWithCategoryUseCase;
            _updateProductUseCase = updateProductUseCase;
            _resolveProductPageStateUseCase = resolveProductPageStateUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public async Task OnGetAsync(long editId = 0)
        {
            var pageParam = Request.Query["pageNumber"].ToString();
            var categoryFilterParam = Request.Query["categoryFilter"].ToString();

            await LoadCategoriesAsync();
            var state = _resolveProductPageStateUseCase.Resolve(pageParam, categoryFilterParam, Categories);
            CurrentPage = state.CurrentPage;
            CategoryFilter = state.CategoryFilter;
            await LoadProductsByState();

            if (NewProduct.Batch == default)
            {
                NewProduct.Batch = DateOnly.FromDateTime(DateTime.Today);
            }

            if (NewProduct.ExpirationDate == default)
            {
                NewProduct.ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
            }
            
            if (editId > 0)
            {
                var editProduct = await _productManagementUseCase.GetForEditAsync(editId, HttpContext.RequestAborted);
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

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewProduct")] CreateProductDto newProduct, int pageNumber = 1, long categoryFilter = 0)
        {
            NewProduct = newProduct;
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            CategoryFilter = categoryFilter;

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
                await _registerNewProductWithCategoryUseCase.ExecuteAsync(NewProduct, HttpContext.RequestAborted);
                
                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToCurrentState();
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

        public async Task<IActionResult> OnPostEditAsync([Bind(Prefix = "EditProduct")] UpdateProductDto editProduct, int pageNumber = 1, long categoryFilter = 0)
        {
            EditProduct = editProduct;
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            CategoryFilter = categoryFilter;

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }

            try
            {
                await _updateProductUseCase.ExecuteAsync(EditProduct, HttpContext.RequestAborted);
                
                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar producto");
                ModelState.AddModelError(string.Empty, validationException.Message);
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar producto");
                ModelState.AddModelError(string.Empty, "Error al actualizar el producto. Intente nuevamente.");
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, int pageNumber = 1, long categoryFilter = 0)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            CategoryFilter = categoryFilter;

            try
            {
                var wasDeleted = await _productManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
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
            return RedirectToCurrentState();
        }

        private RedirectToPageResult RedirectToCurrentState()
        {
            if (CategoryFilter == 0)
            {
                return RedirectToPage(new { pageNumber = CurrentPage });
            }

            return RedirectToPage(new { pageNumber = CurrentPage, categoryFilter = CategoryFilter });
        }

        private async Task LoadProductsByState()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _productManagementUseCase.GetPageAsync(CurrentPage, CategoryFilter, _defaultPageSize, cancellationToken);
                if (CurrentPage > result.TotalPages)
                {
                    CurrentPage = result.TotalPages;
                    result = await _productManagementUseCase.GetPageAsync(CurrentPage, CategoryFilter, _defaultPageSize, cancellationToken);
                }

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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = [.. await _productManagementUseCase.GetCategoriesAsync(HttpContext.RequestAborted)];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
                Categories = [];
            }
        }
    }
}
