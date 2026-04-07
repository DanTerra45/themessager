using System.Globalization;
using Mercadito.src.categories.application.models;
using Mercadito.src.products.application.models;
using Mercadito.src.products.application.ports.input;
using Mercadito.Pages.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel : AppPageModel
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
        private const string CurrentAnchorProductIdSessionKey = "Products.CurrentAnchorProductId";
        private const string PendingNavigationModeSessionKey = "Products.PendingNavigationMode";
        private const string PendingNavigationCursorProductIdSessionKey = "Products.PendingNavigationCursorProductId";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private const string OrderPresetRecent = "recent";
        private const string OrderPresetAlphabeticalAsc = "az";
        private const string OrderPresetAlphabeticalDesc = "za";
        private const string OrderPresetCustom = "custom";
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        private readonly ILogger<ProductsModel> _logger;
        private readonly IProductManagementUseCase _productManagementUseCase;
        private readonly int _defaultPageSize;

        public List<ProductWithCategoriesModel> Products { get; set; } = [];
        public List<CategoryModel> Categories { get; set; } = [];

        public long CategoryFilter { get; set; }

        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorProductId { get; set; }
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

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadProductsByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorProductId);
            }
            else
            {
                await LoadProductsFromAnchorAsync();
            }

            SaveStateInSession();
            EnsureDefaultNewProductValues();
            RestorePendingPostbackState();
            RestoreModelStateErrors(PendingCreateErrorsSessionKey, _logger);
            RestoreModelStateErrors(PendingEditErrorsSessionKey, _logger);

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

            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            ApplyOrderPreset(orderPreset);
            CurrentPage = 1;
            CurrentAnchorProductId = 0;

            ClearPendingEditProductId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "",
            string navigationMode = "",
            long cursorProductId = 0)
        {
            LoadStateFromSession();
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            SetPendingNavigation(navigationMode, cursorProductId);

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
            SetFilterAndState(categoryFilter, currentSortBy, currentSortDirection, searchTerm);
            ToggleSort(sortBy);
            CurrentPage = 1;
            CurrentAnchorProductId = 0;

            ClearPendingEditProductId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEdit(
            long id,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            LoadStateFromSession();
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);

            ClearPendingNavigation();
            SaveStateInSession();
            if (id > 0)
            {
                SetPendingEditProductId(id);
            }

            return RedirectToPage();
        }

    }
}


