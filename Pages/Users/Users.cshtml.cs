using Mercadito.Pages.Infrastructure;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Users
{
    public class UsersModel(
        IGetAllUsersUseCase getAllUsersUseCase,
        IGetAvailableEmployeesUseCase getAvailableEmployeesUseCase,
        IRegisterUserUseCase registerUserUseCase,
        ISendAdministrativePasswordResetLinkUseCase sendAdministrativePasswordResetLinkUseCase,
        IAssignTemporaryPasswordUseCase assignTemporaryPasswordUseCase,
        IDeactivateUserUseCase deactivateUserUseCase,
        ILogger<UsersModel> logger) : AppPageModel
    {
        public CreateUserDto NewUser { get; set; } = new();
        public SendAdministrativePasswordResetLinkDto SendResetLink { get; set; } = new();
        public AssignTemporaryPasswordDto TemporaryPassword { get; set; } = new();

        [BindProperty]
        public long DeactivateUserId { get; set; }

        public IReadOnlyList<UserListItem> ActiveUsers { get; private set; } = [];
        public IReadOnlyList<AvailableEmployeeOption> AvailableEmployees { get; private set; } = [];
        public bool ShowCreateModal { get; private set; }
        public bool ShowSendResetLinkModal { get; private set; }
        public bool ShowTemporaryPasswordModal { get; private set; }
        public bool ShowDeactivateModal { get; private set; }
        public string DeactivateUsername { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = "NewUser")] CreateUserDto newUser)
        {
            ArgumentNullException.ThrowIfNull(newUser);

            NewUser = newUser;
            NewUser.SetupUrlBase = BuildResetUrlBase();

            var actor = BuildAuditActor();
            var result = await registerUserUseCase.ExecuteAsync(NewUser, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, "NewUser");
                ShowCreateModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Usuario registrado y enlace de activación enviado.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendResetLinkAsync([Bind(Prefix = "SendResetLink")] SendAdministrativePasswordResetLinkDto sendResetLink)
        {
            ArgumentNullException.ThrowIfNull(sendResetLink);

            SendResetLink = sendResetLink;
            SendResetLink.ResetUrlBase = BuildResetUrlBase();

            var actor = BuildAuditActor();
            var result = await sendAdministrativePasswordResetLinkUseCase.ExecuteAsync(SendResetLink, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, "SendResetLink");
                ShowSendResetLinkModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = $"Se envió un enlace de restablecimiento a {SendResetLink.Email}. La contraseña actual quedó invalidada.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignTemporaryPasswordAsync([Bind(Prefix = "TemporaryPassword")] AssignTemporaryPasswordDto temporaryPassword)
        {
            ArgumentNullException.ThrowIfNull(temporaryPassword);

            TemporaryPassword = temporaryPassword;

            var actor = BuildAuditActor();
            var result = await assignTemporaryPasswordUseCase.ExecuteAsync(TemporaryPassword, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, "TemporaryPassword");
                TemporaryPassword.Password = string.Empty;
                TemporaryPassword.ConfirmPassword = string.Empty;
                ShowTemporaryPasswordModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = $"Contraseña temporal asignada para {TemporaryPassword.Username}. Debe cambiarla en su próximo inicio de sesión.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeactivateAsync()
        {
            var userId = DeactivateUserId;
            var actor = BuildAuditActor();
            var result = await deactivateUserUseCase.ExecuteAsync(userId, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                if (NeedsDeactivateModal(result.ErrorMessage))
                {
                    ShowDeactivateModal = true;
                    await LoadPageDataAsync();
                    return Page();
                }

                return RedirectToPage();
            }

            TempData["SuccessMessage"] = "Usuario dado de baja correctamente.";
            return RedirectToPage();
        }

        private async Task LoadPageDataAsync()
        {
            var usersResult = await getAllUsersUseCase.ExecuteAsync(HttpContext.RequestAborted);
            if (usersResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el listado de usuarios: {Message}", usersResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el listado de usuarios.";
                ActiveUsers = [];
            }
            else
            {
                ActiveUsers = usersResult.Value;
            }

            DeactivateUsername = ResolveUsername(DeactivateUserId);

            var employeesResult = await getAvailableEmployeesUseCase.ExecuteAsync(HttpContext.RequestAborted);
            if (employeesResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el listado de empleados disponibles: {Message}", employeesResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el listado de empleados disponibles.";
                AvailableEmployees = [];
                return;
            }

            AvailableEmployees = employeesResult.Value;
        }

        private string BuildResetUrlBase()
        {
            return BuildAbsolutePathUrl("/ResetPassword").ToString();
        }

        private string ResolveUsername(long userId)
        {
            if (userId <= 0)
            {
                return string.Empty;
            }

            var matchedUser = ActiveUsers.FirstOrDefault(user => user.Id == userId);
            if (matchedUser == null)
            {
                return string.Empty;
            }

            return matchedUser.Username;
        }

        private static bool NeedsDeactivateModal(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return false;
            }

            return !errorMessage.Contains("propio usuario", StringComparison.OrdinalIgnoreCase)
                && !errorMessage.Contains("administrador", StringComparison.OrdinalIgnoreCase);
        }
    }
}
