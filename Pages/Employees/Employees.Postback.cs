using System.Globalization;
using System.Text.Json;
using Mercadito.src.employees.application.models;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Employees
{
    public partial class EmployeesModel
    {
        private RedirectToPageResult RedirectToCurrentState()
        {
            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateEmployeeDto draft)
        {
            HttpContext.Session.SetString(PendingCreateModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingCreateDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void StorePendingEditModal(UpdateEmployeeDto draft)
        {
            HttpContext.Session.SetString(PendingEditModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingEditDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void RestorePendingPostbackState()
        {
            if (PopFlag(PendingCreateModalSessionKey))
            {
                ShowCreateEmployeeModal = true;
                var pendingCreateDraft = PopDraft<CreateEmployeeDto>(PendingCreateDraftSessionKey);
                if (pendingCreateDraft != null)
                {
                    NewEmployee = pendingCreateDraft;
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingCreateDraftSessionKey);
            }

            if (PopFlag(PendingEditModalSessionKey))
            {
                ShowEditEmployeeModal = true;
                var pendingEditDraft = PopDraft<UpdateEmployeeDto>(PendingEditDraftSessionKey);
                if (pendingEditDraft != null)
                {
                    EditEmployee = pendingEditDraft;
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

        private void SetPendingEditEmployeeId(long employeeId)
        {
            HttpContext.Session.SetString(EditEmployeeSessionKey, employeeId.ToString(CultureInfo.InvariantCulture));
        }

        private long PopPendingEditEmployeeId()
        {
            var rawEditEmployeeId = HttpContext.Session.GetString(EditEmployeeSessionKey);
            HttpContext.Session.Remove(EditEmployeeSessionKey);

            return long.TryParse(rawEditEmployeeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editEmployeeId)
                ? editEmployeeId
                : 0;
        }

        private void ClearPendingEditEmployeeId()
        {
            HttpContext.Session.Remove(EditEmployeeSessionKey);
        }
    }
}

