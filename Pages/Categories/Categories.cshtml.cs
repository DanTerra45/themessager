using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.shared.domain.validation;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel(
        ILogger<CategoriesModel> logger,
        IListingPageStateService listingPageStateService,
        IModalPostbackStateService modalPostbackStateService,
        ICategoryManagementUseCase categoryManagementUseCase,
        IConfiguration configuration) : AppPageModel
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
        private const string SearchTermSessionKey = "Categories.SearchTerm";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private static readonly KeysetListingSessionKeys ListingSessionKeys = new(
            CurrentPageSessionKey,
            CurrentAnchorCategoryIdSessionKey,
            PendingNavigationModeSessionKey,
            PendingNavigationCursorCategoryIdSessionKey,
            SortBySessionKey,
            SortDirectionSessionKey,
            SearchTermSessionKey);
        private static readonly ListingPageStateOptions ListingStateOptions = new(
            ListingSessionKeys,
            DefaultSortBy,
            DefaultSortDirection,
            NormalizeSortBy,
            NormalizeSortDirection,
            ValidationText.NormalizeTrimmed);

        private readonly ILogger<CategoriesModel> _logger = logger;
        private readonly IListingPageStateService _listingPageStateService = listingPageStateService;
        private readonly IModalPostbackStateService _modalPostbackStateService = modalPostbackStateService;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase = categoryManagementUseCase;
        private readonly int _defaultPageSize = PaginationSettings.ResolveDefaultPageSize(configuration);

        public IReadOnlyList<CategoryModel> Categories { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorCategoryId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string SearchTerm { get; set; } = string.Empty;
        public string NextCategoryCodePreview { get; private set; } = "C00001";

        public CreateCategoryDto NewCategory { get; set; } = new CreateCategoryDto { Name = string.Empty, Description = string.Empty, Code = string.Empty };
        public UpdateCategoryDto EditCategory { get; set; } = new UpdateCategoryDto { Name = string.Empty, Description = string.Empty, Code = string.Empty };
        public bool ShowEditCategoryModal { get; set; }
        public bool ShowCreateCategoryModal { get; set; }

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadCategoriesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorId);
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

        public IActionResult OnPostFilter(string sortBy = "", string sortDirection = "", string searchTerm = "", bool clear = false)
        {
            LoadStateFromSession();
        var effectiveSearchTerm = searchTerm;
        if (clear)
        {
            effectiveSearchTerm = string.Empty;
        }

        SetSearchAndSortState(effectiveSearchTerm, sortBy, sortDirection);
            CurrentPage = 1;
            CurrentAnchorCategoryId = 0;

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(
            string navigationMode = "",
            long cursorCategoryId = 0,
            string sortBy = "",
            string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            SetPendingNavigation(navigationMode, cursorCategoryId);

            ClearPendingEditCategoryId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, currentSortBy, currentSortDirection);
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
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
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
