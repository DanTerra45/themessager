using System.Globalization;
using Mercadito.src.categories.domain.model;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.usecases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Mercadito.Pages.Products
{
    public class ProductsModel : PageModel
    {
        private const string CurrentPageSessionKey = "Products.CurrentPage";
        private const string CategoryFilterSessionKey = "Products.CategoryFilter";
        private const string EditProductSessionKey = "Products.EditProductId";
        private const string PendingCreateModalSessionKey = "Products.PendingCreateModal";
        private const string PendingCreateDraftSessionKey = "Products.PendingCreateDraft";
        private const string PendingEditModalSessionKey = "Products.PendingEditModal";
        private const string PendingEditDraftSessionKey = "Products.PendingEditDraft";

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

        public CreateProductDto NewProduct { get; set; } = new CreateProductDto
        {
            Name = string.Empty,
            Description = string.Empty,
            Batch = string.Empty,
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3))
        };

        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto { Name = string.Empty, Description = string.Empty };

        public bool ShowModal { get; set; }

        public bool ShowEditModal { get; set; }

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

        public async Task OnGetAsync()
        {
            LoadStateFromSession();

            await LoadCategoriesAsync();
            NormalizeCurrentState();

            await LoadProductsByState();
            SaveStateInSession();
            EnsureDefaultNewProductDates();
            RestorePendingPostbackState();

            if (ShowModal || ShowEditModal)
            {
                return;
            }

            var editId = PopPendingEditProductId();
            if (editId <= 0)
            {
                return;
            }

            var editProduct = await _productManagementUseCase.GetForEditAsync(editId, HttpContext.RequestAborted);
            if (editProduct != null)
            {
                EditProduct = editProduct;
                ShowEditModal = true;
            }
        }

        public IActionResult OnPostFilterAsync(long categoryFilter = 0)
        {
            SetPageAndFilter(1, categoryFilter);

            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigateAsync(int pageNumber = 1, long categoryFilter = 0)
        {
            SetPageAndFilter(pageNumber, categoryFilter);

            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEditAsync(long id, int pageNumber = 1, long categoryFilter = 0)
        {
            SetPageAndFilter(pageNumber, categoryFilter);

            SaveStateInSession();
            if (id > 0)
            {
                SetPendingEditProductId(id);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewProduct")] CreateProductDto newProduct, int pageNumber = 1, long categoryFilter = 0)
        {
            NewProduct = newProduct;
            SetPageAndFilter(pageNumber, categoryFilter);

            ClearPendingEditProductId();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalido al crear producto");
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewProduct);
                return RedirectToCurrentState();
            }

            try
            {
                await _registerNewProductWithCategoryUseCase.ExecuteAsync(NewProduct, HttpContext.RequestAborted);

                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validacion de negocio al crear producto");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewProduct);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear producto");
                TempData["ErrorMessage"] = "Error al guardar el producto. Intente nuevamente.";
                StorePendingCreateModal(NewProduct);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync([Bind(Prefix = "EditProduct")] UpdateProductDto editProduct, int pageNumber = 1, long categoryFilter = 0)
        {
            EditProduct = editProduct;
            SetPageAndFilter(pageNumber, categoryFilter);
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditProduct);
                return RedirectToCurrentState();
            }

            try
            {
                await _updateProductUseCase.ExecuteAsync(EditProduct, HttpContext.RequestAborted);

                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validacion de negocio al actualizar producto");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingEditModal(EditProduct);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar producto");
                TempData["ErrorMessage"] = "Error al actualizar el producto. Intente nuevamente.";
                StorePendingEditModal(EditProduct);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, int pageNumber = 1, long categoryFilter = 0)
        {
            SetPageAndFilter(pageNumber, categoryFilter);
            SaveStateInSession();

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
            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateProductDto draft)
        {
            HttpContext.Session.SetString(PendingCreateModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingCreateDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void StorePendingEditModal(UpdateProductDto draft)
        {
            HttpContext.Session.SetString(PendingEditModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingEditDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void RestorePendingPostbackState()
        {
            if (PopFlag(PendingCreateModalSessionKey))
            {
                ShowModal = true;
                var pendingCreateDraft = PopDraft<CreateProductDto>(PendingCreateDraftSessionKey);
                if (pendingCreateDraft != null)
                {
                    NewProduct = pendingCreateDraft;
                    EnsureDefaultNewProductDates();
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingCreateDraftSessionKey);
            }

            if (PopFlag(PendingEditModalSessionKey))
            {
                ShowEditModal = true;
                var pendingEditDraft = PopDraft<UpdateProductDto>(PendingEditDraftSessionKey);
                if (pendingEditDraft != null)
                {
                    EditProduct = pendingEditDraft;
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingEditDraftSessionKey);
            }
        }

        private bool PopFlag(string sessionKey)
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            return bool.TryParse(rawValue, out var parsedValue) && parsedValue;
        }

        private T? PopDraft<T>(string sessionKey) where T : class
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(rawValue);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "No se pudo restaurar el borrador temporal de modal para key {SessionKey}", sessionKey);
                return null;
            }
        }

        private void SetPageAndFilter(int pageNumber, long categoryFilter)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            CategoryFilter = categoryFilter >= 0 ? categoryFilter : 0;
        }

        private void NormalizeCurrentState()
        {
            CurrentPage = CurrentPage > 0 ? CurrentPage : 1;
            CategoryFilter = CategoryFilter >= 0 ? CategoryFilter : 0;

            if (CategoryFilter > 0 && Categories.Count > 0 && !Categories.Exists(category => category.Id == CategoryFilter))
            {
                CategoryFilter = 0;
            }
        }

        private async Task LoadProductsByState()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _productManagementUseCase.GetPageAsync(CurrentPage, CategoryFilter, _defaultPageSize, cancellationToken);
                var maxPage = Math.Max(result.TotalPages, 1);

                if (CurrentPage > maxPage)
                {
                    CurrentPage = maxPage;
                    result = await _productManagementUseCase.GetPageAsync(CurrentPage, CategoryFilter, _defaultPageSize, cancellationToken);
                }

                TotalPages = Math.Max(result.TotalPages, 1);
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
                _logger.LogError(exception, "Error al cargar categorias");
                Categories = [];
            }
        }

        private void EnsureDefaultNewProductDates()
        {
            if (string.IsNullOrWhiteSpace(NewProduct.Batch))
            {
                NewProduct.Batch = string.Empty;
            }

            if (NewProduct.ExpirationDate == default)
            {
                NewProduct.ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
            }
        }

        private void LoadStateFromSession()
        {
            var currentPageInSession = HttpContext.Session.GetInt32(CurrentPageSessionKey);
            if (!currentPageInSession.HasValue || currentPageInSession.Value <= 0)
            {
                CurrentPage = 1;
            }
            else
            {
                CurrentPage = currentPageInSession.Value;
            }

            var rawCategoryFilter = HttpContext.Session.GetString(CategoryFilterSessionKey);
            if (!long.TryParse(rawCategoryFilter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCategoryFilter) || parsedCategoryFilter < 0)
            {
                CategoryFilter = 0;
                return;
            }

            CategoryFilter = parsedCategoryFilter;
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
            HttpContext.Session.SetString(CategoryFilterSessionKey, Math.Max(CategoryFilter, 0).ToString(CultureInfo.InvariantCulture));
        }

        private void SetPendingEditProductId(long productId)
        {
            HttpContext.Session.SetString(EditProductSessionKey, productId.ToString(CultureInfo.InvariantCulture));
        }

        private long PopPendingEditProductId()
        {
            var rawEditProductId = HttpContext.Session.GetString(EditProductSessionKey);
            HttpContext.Session.Remove(EditProductSessionKey);

            return long.TryParse(rawEditProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editProductId)
                ? editProductId
                : 0;
        }

        private void ClearPendingEditProductId()
        {
            HttpContext.Session.Remove(EditProductSessionKey);
        }
    }
}
