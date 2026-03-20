using System.Globalization;
using Mercadito.src.categories.domain.model;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.usecases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
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
        private const string PendingCreateErrorsSessionKey = "Products.PendingCreateErrors";
        private const string PendingEditModalSessionKey = "Products.PendingEditModal";
        private const string PendingEditDraftSessionKey = "Products.PendingEditDraft";
        private const string PendingEditErrorsSessionKey = "Products.PendingEditErrors";
        private const string SortBySessionKey = "Products.SortBy";
        private const string SortDirectionSessionKey = "Products.SortDirection";
        private const string SearchTermSessionKey = "Products.SearchTerm";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private const string OrderPresetRecent = "recent";
        private const string OrderPresetAlphabeticalAsc = "az";
        private const string OrderPresetAlphabeticalDesc = "za";
        private const string OrderPresetCustom = "custom";

        private readonly ILogger<ProductsModel> _logger;
        private readonly IProductManagementUseCase _productManagementUseCase;
        private readonly int _defaultPageSize;

        public List<ProductWithCategoriesModel> Products { get; set; } = [];
        public List<CategoryModel> Categories { get; set; } = [];

        public long CategoryFilter { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string SearchTerm { get; set; } = string.Empty;
        public string OrderPreset { get; set; } = OrderPresetAlphabeticalAsc;

        public CreateProductDto NewProduct { get; set; } = new CreateProductDto
        {
            Name = string.Empty,
            Description = string.Empty,
            Stock = 0,
            Batch = string.Empty,
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
            Price = 0.01m
        };

        public UpdateProductDto EditProduct { get; set; } = new UpdateProductDto { Name = string.Empty, Description = string.Empty };

        public bool ShowModal { get; set; }

        public bool ShowEditModal { get; set; }

        public ProductsModel(
            ILogger<ProductsModel> logger,
            IProductManagementUseCase productManagementUseCase,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _productManagementUseCase = productManagementUseCase;
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
            EnsureDefaultNewProductValues();
            RestorePendingPostbackState();
            RestorePendingValidationErrors(PendingCreateErrorsSessionKey);
            RestorePendingValidationErrors(PendingEditErrorsSessionKey);

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

        public IActionResult OnPostFilter(
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "",
            string orderPreset = "",
            bool clear = false)
        {
            if (clear)
            {
                categoryFilter = 0;
                searchTerm = string.Empty;
            }

            SetPageAndFilter(1, categoryFilter, sortBy, sortDirection, searchTerm);
            ApplyOrderPreset(orderPreset);

            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(int pageNumber = 1, long categoryFilter = 0, string sortBy = "", string sortDirection = "", string searchTerm = "")
        {
            SetPageAndFilter(pageNumber, categoryFilter, sortBy, sortDirection, searchTerm);

            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(
            string sortBy = "",
            long categoryFilter = 0,
            string currentSortBy = "",
            string currentSortDirection = "",
            string searchTerm = "")
        {
            SetPageAndFilter(1, categoryFilter, currentSortBy, currentSortDirection, searchTerm);
            ToggleSort(sortBy);

            ClearPendingEditProductId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEdit(
            long id,
            int pageNumber = 1,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            SetPageAndFilter(pageNumber, categoryFilter, sortBy, sortDirection, searchTerm);

            SaveStateInSession();
            if (id > 0)
            {
                SetPendingEditProductId(id);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync(
            [Bind(Prefix = "NewProduct")] CreateProductDto newProduct,
            int pageNumber = 1,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            NewProduct = newProduct;
            SetPageAndFilter(pageNumber, categoryFilter, sortBy, sortDirection, searchTerm);

            ClearPendingEditProductId();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido al crear producto");
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewProduct);
                StorePendingValidationErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _productManagementUseCase.CreateAsync(NewProduct, HttpContext.RequestAborted);

                if (IsRecentOrderPreset(OrderPreset))
                {
                    CurrentPage = 1;
                }

                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al crear producto");
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

        public async Task<IActionResult> OnPostEditAsync(
            [Bind(Prefix = "EditProduct")] UpdateProductDto editProduct,
            int pageNumber = 1,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            EditProduct = editProduct;
            SetPageAndFilter(pageNumber, categoryFilter, sortBy, sortDirection, searchTerm);
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditProduct);
                StorePendingValidationErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _productManagementUseCase.UpdateAsync(EditProduct, HttpContext.RequestAborted);

                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar producto");
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

        public async Task<IActionResult> OnPostDeleteAsync(
            long id,
            int pageNumber = 1,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            SetPageAndFilter(pageNumber, categoryFilter, sortBy, sortDirection, searchTerm);
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
                    EnsureDefaultNewProductValues();
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

        private void StorePendingValidationErrors(string sessionKey)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Valor inválido." : error.ErrorMessage)
                        .ToArray());

            if (errors.Count == 0)
            {
                HttpContext.Session.Remove(sessionKey);
                return;
            }

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(errors));
        }

        private void RestorePendingValidationErrors(string sessionKey)
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            try
            {
                var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(rawValue);
                if (errors == null)
                {
                    return;
                }

                foreach (var (key, messages) in errors)
                {
                    if (messages == null)
                    {
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ModelState.AddModelError(key, message);
                        }
                    }
                }
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "No se pudo restaurar errores de validación para key {SessionKey}", sessionKey);
            }
        }

        private void SetPageAndFilter(int pageNumber, long categoryFilter, string sortBy, string sortDirection, string searchTerm)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            CategoryFilter = categoryFilter >= 0 ? categoryFilter : 0;
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);

            if (string.IsNullOrWhiteSpace(sortBy) && string.IsNullOrWhiteSpace(sortDirection))
            {
                LoadSortStateFromSession();
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            SortBy = NormalizeSortBy(sortBy);
            SortDirection = NormalizeSortDirection(sortDirection);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void NormalizeCurrentState()
        {
            CurrentPage = CurrentPage > 0 ? CurrentPage : 1;
            CategoryFilter = CategoryFilter >= 0 ? CategoryFilter : 0;
            SearchTerm = NormalizeSearchTerm(SearchTerm);
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = NormalizeSortDirection(SortDirection);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);

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
                var result = await _productManagementUseCase.GetPageAsync(
                    CurrentPage,
                    CategoryFilter,
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    SearchTerm,
                    cancellationToken);
                var maxPage = Math.Max(result.TotalPages, 1);

                if (CurrentPage > maxPage)
                {
                    CurrentPage = maxPage;
                    result = await _productManagementUseCase.GetPageAsync(
                        CurrentPage,
                        CategoryFilter,
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        SearchTerm,
                        cancellationToken);
                }

                TotalPages = Math.Max(result.TotalPages, 1);
                Products = [.. result.Products];
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar productos.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar productos.");
                throw;
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
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para productos.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para productos.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
                Categories = [];
            }
        }

        private void EnsureDefaultNewProductValues()
        {
            if (string.IsNullOrWhiteSpace(NewProduct.Batch))
            {
                NewProduct.Batch = string.Empty;
            }

            if (!NewProduct.Stock.HasValue || NewProduct.Stock.Value < 0)
            {
                NewProduct.Stock = 0;
            }

            if (NewProduct.ExpirationDate == default)
            {
                NewProduct.ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
            }

            if (!NewProduct.Price.HasValue || NewProduct.Price.Value < 0.01m)
            {
                NewProduct.Price = 0.01m;
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
            }
            else
            {
                CategoryFilter = parsedCategoryFilter;
            }

            var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
            SearchTerm = NormalizeSearchTerm(persistedSearchTerm is string sessionSearchTerm ? sessionSearchTerm : string.Empty);

            LoadSortStateFromSession();
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
            HttpContext.Session.SetString(CategoryFilterSessionKey, Math.Max(CategoryFilter, 0).ToString(CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(SearchTermSessionKey, NormalizeSearchTerm(SearchTerm));
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
        }

        private void LoadSortStateFromSession()
        {
            var sortByInSession = HttpContext.Session.GetString(SortBySessionKey);
            var sortDirectionInSession = HttpContext.Session.GetString(SortDirectionSessionKey);
            SortBy = NormalizeSortBy(sortByInSession is string persistedSortBy ? persistedSortBy : string.Empty);
            SortDirection = NormalizeSortDirection(sortDirectionInSession is string persistedSortDirection ? persistedSortDirection : string.Empty);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        public string GetSortIcon(string columnName)
        {
            var normalizedColumn = NormalizeSortBy(columnName);
            if (!string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return "bi-arrow-down-up";
            }

            return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "bi-sort-down"
                : "bi-sort-up";
        }

        private void ToggleSort(string sortBy)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            if (string.Equals(SortBy, normalizedSortBy, StringComparison.OrdinalIgnoreCase))
            {
                SortDirection = string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            SortBy = normalizedSortBy;
            SortDirection = DefaultSortDirection;
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void ApplyOrderPreset(string orderPreset)
        {
            var normalizedOrderPreset = NormalizeOrderPreset(orderPreset);
            if (string.IsNullOrWhiteSpace(normalizedOrderPreset))
            {
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetCustom, StringComparison.Ordinal))
            {
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetRecent, StringComparison.Ordinal))
            {
                SortBy = "id";
                SortDirection = "desc";
                OrderPreset = OrderPresetRecent;
                CurrentPage = 1;
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalAsc, StringComparison.Ordinal))
            {
                SortBy = "name";
                SortDirection = "asc";
                OrderPreset = OrderPresetAlphabeticalAsc;
                CurrentPage = 1;
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalDesc, StringComparison.Ordinal))
            {
                SortBy = "name";
                SortDirection = "desc";
                OrderPreset = OrderPresetAlphabeticalDesc;
                CurrentPage = 1;
                return;
            }

            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private static string NormalizeOrderPreset(string orderPreset)
        {
            if (string.IsNullOrWhiteSpace(orderPreset))
            {
                return string.Empty;
            }

            var normalizedOrderPreset = orderPreset.Trim().ToLowerInvariant();
            if (string.Equals(normalizedOrderPreset, OrderPresetRecent, StringComparison.Ordinal))
            {
                return OrderPresetRecent;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalAsc, StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalAsc;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalDesc, StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalDesc;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetCustom, StringComparison.Ordinal))
            {
                return OrderPresetCustom;
            }

            return string.Empty;
        }

        private static string ResolveOrderPreset(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var normalizedSortDirection = NormalizeSortDirection(sortDirection);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "desc", StringComparison.Ordinal))
            {
                return OrderPresetRecent;
            }

            if (string.Equals(normalizedSortBy, "name", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "asc", StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalAsc;
            }

            if (string.Equals(normalizedSortBy, "name", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "desc", StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalDesc;
            }

            return OrderPresetCustom;
        }

        private static bool IsRecentOrderPreset(string orderPreset)
        {
            return string.Equals(orderPreset, OrderPresetRecent, StringComparison.Ordinal);
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return DefaultSortBy;
            }

            var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "id" => "id",
                "stock" => "stock",
                "batch" => "batch",
                "expirationdate" => "expirationdate",
                "price" => "price",
                _ => "name"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
        }

        private string ResolveSearchTermFromRequest(string searchTerm)
        {
            var hasSearchTermInForm = Request.HasFormContentType && Request.Form.ContainsKey("searchTerm");
            var hasSearchTermInQuery = Request.Query.ContainsKey("searchTerm");

            if (hasSearchTermInForm || hasSearchTermInQuery)
            {
                return NormalizeSearchTerm(searchTerm);
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
                return NormalizeSearchTerm(persistedSearchTerm is string sessionSearchTerm ? sessionSearchTerm : string.Empty);
            }

            return NormalizeSearchTerm(searchTerm);
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return string.Empty;
            }

            return searchTerm.Trim();
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
