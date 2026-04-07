using System.Security.Claims;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Users
{
    public class UsersModel : AppPageModel
    {
        private readonly IGetAllUsersUseCase _getAllUsersUseCase;
        private readonly IGetAvailableEmployeesUseCase _getAvailableEmployeesUseCase;
        private readonly IRegisterUserUseCase _registerUserUseCase;
        private readonly IResetUserPasswordUseCase _resetUserPasswordUseCase;
        private readonly IDeactivateUserUseCase _deactivateUserUseCase;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(
            IGetAllUsersUseCase getAllUsersUseCase,
            IGetAvailableEmployeesUseCase getAvailableEmployeesUseCase,
            IRegisterUserUseCase registerUserUseCase,
            IResetUserPasswordUseCase resetUserPasswordUseCase,
            IDeactivateUserUseCase deactivateUserUseCase,
            ILogger<UsersModel> logger)
        {
            _getAllUsersUseCase = getAllUsersUseCase;
            _getAvailableEmployeesUseCase = getAvailableEmployeesUseCase;
            _registerUserUseCase = registerUserUseCase;
            _resetUserPasswordUseCase = resetUserPasswordUseCase;
            _deactivateUserUseCase = deactivateUserUseCase;
            _logger = logger;
        }

        [BindProperty]
        public CreateUserDto NewUser { get; set; } = new();

        [BindProperty]
        public ResetUserPasswordDto ResetPassword { get; set; } = new();

        public IReadOnlyList<UserListItem> ActiveUsers { get; private set; } = [];
        public IReadOnlyList<AvailableEmployeeOption> AvailableEmployees { get; private set; } = [];
        public bool ShowCreateModal { get; private set; }
        public bool ShowResetPasswordModal { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            NewUser.SetupUrlBase = BuildResetUrlBase();
            var actor = BuildAuditActor();
            var result = await _registerUserUseCase.ExecuteAsync(NewUser, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                if (result.Errors.Count == 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToPage();
                }

                ApplyResultErrors(result, "NewUser");
                ShowCreateModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Usuario registrado y enlace de activación enviado.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync()
        {
            var actor = BuildAuditActor();
            var result = await _resetUserPasswordUseCase.ExecuteAsync(ResetPassword, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                if (result.Errors.Count == 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToPage();
                }

                ApplyResultErrors(result, "ResetPassword");
                ResetPassword.Password = string.Empty;
                ResetPassword.ConfirmPassword = string.Empty;
                ShowResetPasswordModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = $"Contraseña restablecida para {ResetPassword.Username}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(long userId)
        {
            var actor = BuildAuditActor();
            var result = await _deactivateUserUseCase.ExecuteAsync(userId, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToPage();
            }

            TempData["SuccessMessage"] = "Usuario dado de baja correctamente.";
            return RedirectToPage();
        }

        private async Task LoadPageDataAsync()
        {
            var usersResult = await _getAllUsersUseCase.ExecuteAsync(HttpContext.RequestAborted);
            if (usersResult.IsFailure)
            {
                _logger.LogError("No se pudo cargar el listado de usuarios: {Message}", usersResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el listado de usuarios.";
                ActiveUsers = [];
            }
            else
            {
                ActiveUsers = usersResult.Value;
            }

            var employeesResult = await _getAvailableEmployeesUseCase.ExecuteAsync(HttpContext.RequestAborted);
            if (employeesResult.IsFailure)
            {
                _logger.LogError("No se pudo cargar el listado de empleados disponibles: {Message}", employeesResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el listado de empleados disponibles.";
                AvailableEmployees = [];
                return;
            }

            AvailableEmployees = employeesResult.Value;
        }

        private string BuildResetUrlBase()
        {
            return BuildAbsolutePathUrl("/ResetPassword");
        }
    }
}
