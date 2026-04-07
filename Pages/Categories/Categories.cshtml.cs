using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Mercadito.Pages.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Globalization;
using System.Text.Json;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel : AppPageModel
    {
        private const string CurrentPageSessionKey = "Categories.CurrentPage";
        private const string CurrentAnchorCategoryIdSessionKey = "Categories.CurrentAnchorCategoryId";
        private const string PendingNavigationModeSessionKey = "Categories.PendingNavigationMode";
        private const string PendingNavigationCursorCategoryIdSessionKey = "Categories.PendingNavigationCursorCategoryId";
        private const string EditCategorySessionKey = "Categories.EditCategoryId";
        private const string PendingCreateModalSessionKey = "Categories.PendingCreateModal";
        private const string PendingCreateDraftSessionKey = "Categories.PendingCreateDraft";
        private const string PendingCreateErrorsSessionKey = "Categories.PendingCreateErrors";
        private const string PendingEditModalSessionKey = "Categories.PendingEditModal";
        private const string PendingEditDraftSessionKey = "Categories.PendingEditDraft";
        private const string PendingEditErrorsSessionKey = "Categories.PendingEditErrors";
        private const string SortBySessionKey = "Categories.SortBy";
        private const string SortDirectionSessionKey = "Categories.SortDirection";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        private readonly ILogger<CategoriesModel> _logger;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase;
        private readonly int _defaultPageSize;

        public List<CategoryModel> Categories { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorCategoryId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string NextCategoryCodePreview { get; private set; } = "C00001";

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

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadCategoriesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorCategoryId);
            }
            else
            {
                await LoadCategoriesFromAnchorAsync();
            }

            SaveStateInSession();
            RestorePendingPostbackState();
            RestoreModelStateErrors(PendingCreateErrorsSessionKey, _logger);
            RestoreModelStateErrors(PendingEditErrorsSessionKey, _logger);
            await LoadNextCategoryCodePreviewAsync();
            NewCategory.Code = NextCategoryCodePreview;

            if (ShowCreateCategoryModal || ShowEditCategoryModal)
            {
                return;
            }

            var editCategoryId = PopPendingEditCategoryId();
            if (editCategoryId <= 0)
            {
                return;
            }

            var categoryForEdit = await _categoryManagementUseCase.GetForEditAsync(editCategoryId, HttpContext.RequestAborted);
            if (categoryForEdit != null)
            {
                EditCategory = categoryForEdit;
                ShowEditCategoryModal = true;
            }
        }

        public IActionResult OnPostNavigate(
            string navigationMode = "",
            long cursorCategoryId = 0,
            string sortBy = "",
            string sortDirection = "")
        {
            LoadStateFromSession();
            SetSortState(sortBy, sortDirection);
            SetPendingNavigation(navigationMode, cursorCategoryId);

            ClearPendingEditCategoryId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            SetSortState(currentSortBy, currentSortDirection);
            ToggleSort(sortBy);
            CurrentPage = 1;
            CurrentAnchorCategoryId = 0;

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEdit(long id, string sortBy = "", string sortDirection = "")
        {
            LoadStateFromSession();
            SetSortState(sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (id > 0)
            {
                SetPendingEditCategoryId(id);
            }

            return RedirectToPage();
        }

    }
}


