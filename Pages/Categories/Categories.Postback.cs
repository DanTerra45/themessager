using Mercadito.src.application.categories.models;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel
    {
        private RedirectToPageResult RedirectToCurrentState()
        {
            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateCategoryDto draft)
        {
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingCreateModalSessionKey, PendingCreateDraftSessionKey, draft);
        }

        private void StorePendingEditModal(UpdateCategoryDto draft)
        {
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingEditModalSessionKey, PendingEditDraftSessionKey, draft);
        }

        private void RestorePendingPostbackState()
        {
            var createState = _modalPostbackStateService.RestorePendingModalDraft<CreateCategoryDto>(
                HttpContext.Session,
                PendingCreateModalSessionKey,
                PendingCreateDraftSessionKey,
                _logger);
            if (createState.ShouldShowModal)
            {
                ShowCreateCategoryModal = true;
                if (createState.Draft != null)
                {
                    NewCategory = createState.Draft;
                }
            }

            var editState = _modalPostbackStateService.RestorePendingModalDraft<UpdateCategoryDto>(
                HttpContext.Session,
                PendingEditModalSessionKey,
                PendingEditDraftSessionKey,
                _logger);
            if (editState.ShouldShowModal)
            {
                ShowEditCategoryModal = true;
                if (editState.Draft != null)
                {
                    EditCategory = editState.Draft;
                }
            }
        }

        private void SetPendingEditCategoryId(long categoryId)
        {
            _modalPostbackStateService.SetPendingEntityId(HttpContext.Session, EditCategorySessionKey, categoryId);
        }

        private long PopPendingEditCategoryId()
        {
            return _modalPostbackStateService.PopPendingEntityId(HttpContext.Session, EditCategorySessionKey);
        }

        private void ClearPendingEditCategoryId()
        {
            _modalPostbackStateService.ClearPendingEntityId(HttpContext.Session, EditCategorySessionKey);
        }
    }
}
