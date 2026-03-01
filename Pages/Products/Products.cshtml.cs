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
        private readonly RegisterNewProductUseCase _registerNewProductUseCase;
        private readonly RegisterNewProductWithCategoryUseCase _registerNewProductWithCategoryUseCase;
        public List<ProductWithCategoriesModel> Products { get; set; } = new List<ProductWithCategoriesModel>();
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        [BindProperty]
        public RegisterNewProductWithCategoryDto NewProduct { get; set; } = new RegisterNewProductWithCategoryDto();

        [BindProperty]
        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto();

        public ProductsModel(ILogger<ProductsModel> logger, IProductRepository productRepository, ICategoryRepository categoryRepository, RegisterNewProductUseCase registerNewProductUseCase, RegisterNewProductWithCategoryUseCase registerNewProductWithCategoryUseCase)
        {
            _logger = logger;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _registerNewProductUseCase = registerNewProductUseCase;
            _registerNewProductWithCategoryUseCase = registerNewProductWithCategoryUseCase;
        }

        public async Task OnGetAsync()
        {
            await LoadProductsAsync();
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                if(NewProduct.CategoryId == Guid.Empty)
                {
                    var createProductDto = ProductMapper.ToRegisterProductEntity(NewProduct);
                    await _registerNewProductUseCase.ExecuteAsync(createProductDto);
                }
                else
                {
                    await _registerNewProductWithCategoryUseCase.ExecuteAsync(NewProduct);
                }
                _logger.LogInformation("Producto creado: {Name}", NewProduct.Name);

                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                ModelState.AddModelError(string.Empty, "Error al guardar el producto. Intente nuevamente.");
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                return Page();
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var resultado = await _productRepository.GetAllProductsWithCategoriesAsync();
                TotalPages = (int)Math.Ceiling(resultado.Count() / 10.0);
                resultado = await _productRepository.GetProductsWithCategoriesByPages(CurrentPage);
                Products = resultado?.ToList() ?? new List<ProductWithCategoriesModel>();
                if (Products.Count == 0)
                {
                    _logger.LogWarning("No se encontraron productos en la base de datos.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar productos");
                Products = new List<ProductWithCategoriesModel>();
            }
        }

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

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                TempData["ShowEditProductModal"] = "true";
                return Page();
            }

            try
            {
                var existing = await _productRepository.GetProductByIdAsync(EditProduct.Id);
                if (existing == null)
                {
                    ModelState.AddModelError(string.Empty, "Producto no encontrado.");
                    await LoadProductsAsync();
                    await LoadCategoriesAsync();
                    return Page();
                }

                var updated = new Product
                {
                    Id = EditProduct.Id,
                    Name = EditProduct.Name,
                    Description = EditProduct.Description ?? string.Empty,
                    Stock = EditProduct.Stock,
                    Lote = EditProduct.Lote,
                    FechaDeCaducidad = EditProduct.FechaDeCaducidad,
                    Price = EditProduct.Price
                };

                await _productRepository.UpdateProductAsync(updated);
                _logger.LogInformation("Producto actualizado: {Id}", EditProduct.Id);
                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto");
                ModelState.AddModelError(string.Empty, "Error al actualizar el producto. Intente nuevamente.");
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _productRepository.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "Producto eliminado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto");
                TempData["ErrorMessage"] = "No se pudo eliminar el producto.";
            }
            return RedirectToPage();
        }
    }
}