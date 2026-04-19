using Mercadito.src.application.employees.models;
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
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingCreateModalSessionKey, PendingCreateDraftSessionKey, draft);
        }

        private void StorePendingEditModal(UpdateEmployeeDto draft)
        {
            _modalPostbackStateService.StorePendingModalDraft(HttpContext.Session, PendingEditModalSessionKey, PendingEditDraftSessionKey, draft);
        }

        private void RestorePendingPostbackState()
        {
            var createState = _modalPostbackStateService.RestorePendingModalDraft<CreateEmployeeDto>(
                HttpContext.Session,
                PendingCreateModalSessionKey,
                PendingCreateDraftSessionKey,
                _logger);
            if (createState.ShouldShowModal)
            {
                ShowCreateEmployeeModal = true;
                if (createState.Draft != null)
                {
                    NewEmployee = createState.Draft;
                }
            }

            var editState = _modalPostbackStateService.RestorePendingModalDraft<UpdateEmployeeDto>(
                HttpContext.Session,
                PendingEditModalSessionKey,
                PendingEditDraftSessionKey,
                _logger);
            if (editState.ShouldShowModal)
            {
                ShowEditEmployeeModal = true;
                if (editState.Draft != null)
                {
                    EditEmployee = editState.Draft;
                }
            }
        }

        private void SetPendingEditEmployeeId(long employeeId)
        {
            _modalPostbackStateService.SetPendingEntityId(HttpContext.Session, EditEmployeeSessionKey, employeeId);
        }

        private long PopPendingEditEmployeeId()
        {
            return _modalPostbackStateService.PopPendingEntityId(HttpContext.Session, EditEmployeeSessionKey);
        }

        private void ClearPendingEditEmployeeId()
        {
            _modalPostbackStateService.ClearPendingEntityId(HttpContext.Session, EditEmployeeSessionKey);
        }
    }
}
