using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.usecases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace Mercadito.Pages.Categories
{
    public class CategoriesModel : PageModel
    {
        private const string CurrentPageSessionKey = "Categories.CurrentPage";
        private const string EditCategorySessionKey = "Categories.EditCategoryId";
        private const string PendingCreateModalSessionKey = "Categories.PendingCreateModal";
        private const string PendingCreateDraftSessionKey = "Categories.PendingCreateDraft";
        private const string PendingEditModalSessionKey = "Categories.PendingEditModal";
        private const string PendingEditDraftSessionKey = "Categories.PendingEditDraft";

        private readonly ILogger<CategoriesModel> _logger;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase;
        private readonly int _defaultPageSize;

        public List<CategoryModel> Categories { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

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
            LoadCurrentPageFromSession();
            await LoadCategoriesAsync();
            SaveCurrentPageInSession();
            RestorePendingPostbackState();

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

        public IActionResult OnPostNavigateAsync(int pageNumber = 1)
        {
            SetCurrentPage(pageNumber);

            ClearPendingEditCategoryId();
            SaveCurrentPageInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEditAsync(long id, int pageNumber = 1)
        {
            SetCurrentPage(pageNumber);
            SaveCurrentPageInSession();

            if (id > 0)
            {
                SetPendingEditCategoryId(id);
            }

            return RedirectToPage();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _categoryManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, cancellationToken);
                var maxPage = Math.Max(result.TotalPages, 1);

                if (CurrentPage > maxPage)
                {
                    CurrentPage = maxPage;
                    result = await _categoryManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, cancellationToken);
                }

                TotalPages = Math.Max(result.TotalPages, 1);
                Categories = [.. result.Categories];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorias");
                ModelState.AddModelError(string.Empty, "Error al cargar las categorias. Intente nuevamente.");
                Categories = [];
                TotalPages = 1;
            }
        }

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewCategory")] CreateCategoryDto newCategory, int pageNumber = 1)
        {
            NewCategory = newCategory;
            SetCurrentPage(pageNumber);

            ClearPendingEditCategoryId();
            SaveCurrentPageInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewCategory);
                return RedirectToCurrentState();
            }

            try
            {
                await _categoryManagementUseCase.CreateAsync(NewCategory, HttpContext.RequestAborted);
                _logger.LogInformation("Categoria creada: {Name}", NewCategory.Name);

                TempData["SuccessMessage"] = "Categoria agregada exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Business validation while creating category");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewCategory);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear categoria");
                TempData["ErrorMessage"] = "Error al guardar la categoria. Intente nuevamente.";
                StorePendingCreateModal(NewCategory);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync([Bind(Prefix = "EditCategory")] UpdateCategoryDto editCategory, int pageNumber = 1)
        {
            EditCategory = editCategory;
            SetCurrentPage(pageNumber);
            SaveCurrentPageInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }

            try
            {
                await _categoryManagementUseCase.UpdateAsync(EditCategory, HttpContext.RequestAborted);
                _logger.LogInformation("Categoria actualizada: {Id}", EditCategory.Id);
                TempData["SuccessMessage"] = "Categoria actualizada correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validacion de negocio al actualizar categoria");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar categoria");
                TempData["ErrorMessage"] = "Error al actualizar la categoria. Intente nuevamente.";
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, int pageNumber = 1)
        {
            SetCurrentPage(pageNumber);

            ClearPendingEditCategoryId();
            SaveCurrentPageInSession();

            try
            {
                var wasDeleted = await _categoryManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Categoria desactivada.";
                }
                else
                {
                    TempData["ErrorMessage"] = "La categoria no existe o ya estaba desactivada.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar la categoria");
                TempData["ErrorMessage"] = "No se pudo eliminar la categoria.";
            }

            return RedirectToCurrentState();
        }

        private RedirectToPageResult RedirectToCurrentState()
        {
            ClearPendingEditCategoryId();
            SaveCurrentPageInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateCategoryDto draft)
        {
            HttpContext.Session.SetString(PendingCreateModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingCreateDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void StorePendingEditModal(UpdateCategoryDto draft)
        {
            HttpContext.Session.SetString(PendingEditModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingEditDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void RestorePendingPostbackState()
        {
            if (PopFlag(PendingCreateModalSessionKey))
            {
                ShowCreateCategoryModal = true;
                var pendingCreateDraft = PopDraft<CreateCategoryDto>(PendingCreateDraftSessionKey);
                if (pendingCreateDraft != null)
                {
                    NewCategory = pendingCreateDraft;
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingCreateDraftSessionKey);
            }

            if (PopFlag(PendingEditModalSessionKey))
            {
                ShowEditCategoryModal = true;
                var pendingEditDraft = PopDraft<UpdateCategoryDto>(PendingEditDraftSessionKey);
                if (pendingEditDraft != null)
                {
                    EditCategory = pendingEditDraft;
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

        private void LoadCurrentPageFromSession()
        {
            var currentPageInSession = HttpContext.Session.GetInt32(CurrentPageSessionKey);
            if (!currentPageInSession.HasValue || currentPageInSession.Value <= 0)
            {
                CurrentPage = 1;
                return;
            }

            CurrentPage = currentPageInSession.Value;
        }

        private void SetCurrentPage(int pageNumber)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
        }

        private void SaveCurrentPageInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
        }

        private void SetPendingEditCategoryId(long categoryId)
        {
            HttpContext.Session.SetString(EditCategorySessionKey, categoryId.ToString(CultureInfo.InvariantCulture));
        }

        private long PopPendingEditCategoryId()
        {
            var rawEditCategoryId = HttpContext.Session.GetString(EditCategorySessionKey);
            HttpContext.Session.Remove(EditCategorySessionKey);

            return long.TryParse(rawEditCategoryId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editCategoryId)
                ? editCategoryId
                : 0;
        }

        private void ClearPendingEditCategoryId()
        {
            HttpContext.Session.Remove(EditCategorySessionKey);
        }
    }
}
