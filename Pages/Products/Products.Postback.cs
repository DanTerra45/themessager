using Mercadito.src.products.application.models;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel
    {
        private RedirectToPageResult RedirectToCurrentState()
        {
            ClearPendingEditProductId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateProductDto draft)
        {
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingCreateModalSessionKey, PendingCreateDraftSessionKey, draft);
        }

        private void StorePendingEditModal(UpdateProductDto draft)
        {
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingEditModalSessionKey, PendingEditDraftSessionKey, draft);
        }

        private void RestorePendingPostbackState()
        {
            var createState = _modalPostbackStateService.RestorePendingModalDraft<CreateProductDto>(
                HttpContext.Session,
                PendingCreateModalSessionKey,
                PendingCreateDraftSessionKey,
                _logger);
            if (createState.ShouldShowModal)
            {
                ShowModal = true;
                if (createState.Draft != null)
                {
                    NewProduct = createState.Draft;
                    EnsureDefaultNewProductValues();
                }
            }

            var editState = _modalPostbackStateService.RestorePendingModalDraft<UpdateProductDto>(
                HttpContext.Session,
                PendingEditModalSessionKey,
                PendingEditDraftSessionKey,
                _logger);
            if (editState.ShouldShowModal)
            {
                ShowEditModal = true;
                if (editState.Draft != null)
                {
                    EditProduct = editState.Draft;
                }
            }
        }

        private void SetPendingEditProductId(long productId)
        {
            _modalPostbackStateService.SetPendingEntityId(HttpContext.Session, EditProductSessionKey, productId);
        }

        private long PopPendingEditProductId()
        {
            return _modalPostbackStateService.PopPendingEntityId(HttpContext.Session, EditProductSessionKey);
        }

        private void ClearPendingEditProductId()
        {
            _modalPostbackStateService.ClearPendingEntityId(HttpContext.Session, EditProductSessionKey);
        }
    }
}
