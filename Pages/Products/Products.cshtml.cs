using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Mercadito.src.products.data.dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Mercadito.Pages.Products
{
    public class ProductsModel : PageModel
    {
        private readonly ILogger<ProductsModel> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly RegisterNewProductUseCase _registerNewProductUseCase;
        private readonly RegisterNewProductWithCategoryUseCase _registerNewProductWithCategoryUseCase;
        private readonly AsignCategoryToProductUseCase _asignCategoryToProductUseCase;
        private readonly UpdateProductUseCase _updateProductUseCase;

        public List<ProductWithCategoriesModel> Products { get; set; } = new List<ProductWithCategoriesModel>();
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();

        public long? CategoryFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public RegisterNewProductWithCategoryDto NewProduct { get; set; } = new RegisterNewProductWithCategoryDto();
        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto();

        [TempData]
        public bool ShowModal { get; set; }

        public ProductsModel(
            ILogger<ProductsModel> logger,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IProductCategoryRepository productCategoryRepository,
            RegisterNewProductUseCase registerNewProductUseCase,
            RegisterNewProductWithCategoryUseCase registerNewProductWithCategoryUseCase,
            AsignCategoryToProductUseCase asignCategoryToProductUseCase,
            UpdateProductUseCase updateProductUseCase
            )
        {
            _logger = logger;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _productCategoryRepository = productCategoryRepository;
            _registerNewProductUseCase = registerNewProductUseCase;
            _registerNewProductWithCategoryUseCase = registerNewProductWithCategoryUseCase;
            _asignCategoryToProductUseCase = asignCategoryToProductUseCase;
            _updateProductUseCase = updateProductUseCase;
        }

        public async Task OnGetAsync(long? editId)
        {
            var pageParam = Request.Query["page"].ToString();
            var categoryFilterParam = Request.Query["categoryFilter"].ToString();
            await LoadCategoriesAsync();
            await HandleGetRequest(pageParam, categoryFilterParam);
            
            if (editId.HasValue)
            {
                var product = await _productRepository.GetProductByIdAsync(editId.Value);
                if (product != null)
                {
                    var relation = await _productCategoryRepository.GetProductsCategoriesByProductIdAsync(editId.Value);
                    
                    EditProduct = new UpdateProductDto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Stock = product.Stock,
                        Lote = product.Lote,
                        FechaDeCaducidad = product.FechaDeCaducidad,
                        Price = product.Price,
                        CategoryId = relation?.CategoryId ?? 0
                    };
                }
            }
        }

        public async Task<IActionResult> OnPostFilterAsync(long? categoryFilter)
        {
            CategoryFilter = categoryFilter;
            CurrentPage = 1;
            
            await LoadCategoriesAsync();
            await LoadProductsByState();
            
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(RegisterNewProductWithCategoryDto newProduct)
        {
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
                NewProduct = newProduct;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }

            try
            {
                if(newProduct.CategoryId == 0)
                {
                    var createProductDto = ProductMapper.ToRegisterProductEntity(newProduct);
                    await _registerNewProductUseCase.ExecuteAsync(createProductDto);
                }
                else
                {
                    await _registerNewProductWithCategoryUseCase.ExecuteAsync(newProduct);
                }
                
                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                ModelState.AddModelError(string.Empty, "Error al guardar el producto. Intente nuevamente.");
                ShowModal = true;
                NewProduct = newProduct;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(UpdateProductDto editProduct)
        {
            if (!ModelState.IsValid)
            {
                EditProduct = editProduct;
                await LoadCategoriesAsync();
                await LoadProductsByState();
                TempData["ShowEditProductModal"] = "true";
                return Page();
            }

            try
            {
                var existing = await _productRepository.GetProductByIdAsync(editProduct.Id);
                if (existing == null)
                {
                    ModelState.AddModelError(string.Empty, "Producto no encontrado.");
                    EditProduct = editProduct;
                    await LoadCategoriesAsync();
                    await LoadProductsByState();
                    TempData["ShowEditProductModal"] = "true";
                    return Page();
                }

                await _updateProductUseCase.ExecuteAsync(editProduct);
                
                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto");
                ModelState.AddModelError(string.Empty, "Error al actualizar el producto. Intente nuevamente.");
                EditProduct = editProduct;
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
                var hasRelation = await _productCategoryRepository.GetProductsCategoriesByProductIdAsync(id) != null;
                if (hasRelation)
                {
                    await _productCategoryRepository.DeleteProductCategoriesByProductIdAsync(id);
                }

                await _productRepository.DeleteProductAsync(id);
                TempData["SuccessMessage"] = hasRelation
                    ? "Producto eliminado. También se eliminó su relación con categoría."
                    : "Producto eliminado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto");
                TempData["ErrorMessage"] = "No se pudo eliminar el producto.";
            }
            return RedirectToPage();
        }

        #region Métodos Privados - Manejo de Estados

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
            
            CategoryFilter = null;
            await LoadAllProductsPaginated();
        }

        private async Task HandleCategoryFilterOnly(string categoryFilterParam)
        {
            CurrentPage = 1;
            CategoryFilter = await ResolveCategoryFilter(categoryFilterParam);
            
            if (CategoryFilter.HasValue)
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
            
            CategoryFilter = await ResolveCategoryFilter(categoryFilterParam);
            
            if (CategoryFilter.HasValue)
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
            CategoryFilter = null;
            await LoadAllProductsPaginated();
        }

        private async Task<long?> ResolveCategoryFilter(string categoryFilterParam)
        {
            if (string.IsNullOrEmpty(categoryFilterParam))
            {
                return null;
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
            return null;
        }

        private async Task LoadProductsByState()
        {
            if (CategoryFilter.HasValue && CategoryFilter.Value != 0)
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
                var totalCount = await _productRepository.GetTotalProductsCountAsync();
                TotalPages = (int)Math.Ceiling(totalCount / 10.0);
                
                var productos = await _productRepository.GetProductsWithCategoriesByPages(CurrentPage);
                Products = productos?.ToList() ?? new List<ProductWithCategoriesModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar productos");
                Products = new List<ProductWithCategoriesModel>();
                TotalPages = 1;
            }
        }

        private async Task LoadProductsByCategoryPaginated()
        {
            try
            {
                if (!CategoryFilter.HasValue || CategoryFilter.Value == 0)
                {
                    await LoadAllProductsPaginated();
                    return;
                }
                
                var totalCount = await _productRepository.GetTotalProductsCountByCategoryAsync(CategoryFilter.Value);
                TotalPages = (int)Math.Ceiling(totalCount / 10.0);
                
                var productos = await _productRepository.GetProductsWithCategoriesFilterByCategoryByPages(
                    CurrentPage, CategoryFilter.Value);
                Products = productos?.ToList() ?? new List<ProductWithCategoriesModel>();
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar productos filtrados");
                Products = new List<ProductWithCategoriesModel>();
                TotalPages = 1;
            }
        }

        #endregion

        #region Métodos Privados - Auxiliares

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllCategoriesAsync();
                Categories = categories?.ToList() ?? new List<CategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categorías");
                Categories = new List<CategoryModel>();
            }
        }

        #endregion
    }
}