using System.Globalization;
using System.Text.Json;
using Mercadito.src.categories.domain.dto;
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
