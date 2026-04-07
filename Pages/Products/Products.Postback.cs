using System.Globalization;
using System.Text.Json;
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

