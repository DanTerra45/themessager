using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Users;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Users;

public sealed class IndexModel(IUsersApiAdapter usersApiAdapter, ILogger<IndexModel> logger) : FrontendPageModel
{
    [BindProperty]
    public RegisterUserRequestDto NewUser { get; set; } = new();

    [BindProperty]
    public SendPasswordResetLinkRequestDto SendResetLink { get; set; } = new();

    [BindProperty]
    public AssignTemporaryPasswordRequestDto TemporaryPassword { get; set; } = new();

    [BindProperty]
    public long DeactivateUserId { get; set; }

    public IReadOnlyList<UserSummaryDto> ActiveUsers { get; private set; } = [];
    public IReadOnlyList<AvailableEmployeeDto> AvailableEmployees { get; private set; } = [];
    public bool ShowCreateModal { get; private set; }
    public bool ShowSendResetLinkModal { get; private set; }
    public bool ShowTemporaryPasswordModal { get; private set; }
    public bool ShowDeactivateModal { get; private set; }
    public string DeactivateUsername { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        NewUser.SetupUrlBase = BuildResetUrlBase();

        var result = await usersApiAdapter.RegisterUserAsync(
            NewUser,
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(NewUser));
            ShowCreateModal = true;
            await LoadPageDataAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "Usuario registrado y enlace de activación enviado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendResetLinkAsync()
    {
        SendResetLink.ResetUrlBase = BuildResetUrlBase();

        var result = await usersApiAdapter.SendResetLinkAsync(
            SendResetLink.UserId,
            SendResetLink,
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(SendResetLink));
            ShowSendResetLinkModal = true;
            await LoadPageDataAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Se envió un enlace de restablecimiento a {SendResetLink.Email}. La contraseña actual quedó invalidada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignTemporaryPasswordAsync()
    {
        var result = await usersApiAdapter.AssignTemporaryPasswordAsync(
            TemporaryPassword.UserId,
            TemporaryPassword,
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ApplyTemporaryPasswordErrors(result);
            TemporaryPassword.TemporaryPassword = string.Empty;
            TemporaryPassword.ConfirmTemporaryPassword = string.Empty;
            ShowTemporaryPasswordModal = true;
            await LoadPageDataAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Contraseña temporal asignada para {TemporaryPassword.Username}. Debe cambiarla en su próximo inicio de sesión.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync()
    {
        var result = await usersApiAdapter.DeactivateUserAsync(
            DeactivateUserId,
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo dar de baja al usuario.");

            if (NeedsDeactivateModal(TempData["ErrorMessage"]?.ToString()))
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
        var usersResult = await usersApiAdapter.GetUsersAsync(HttpContext.RequestAborted);
        if (usersResult.Success && usersResult.Data != null)
        {
            ActiveUsers = usersResult.Data;
        }
        else
        {
            logger.LogWarning("No se pudo cargar el listado de usuarios: {Errors}", string.Join(" | ", usersResult.Errors));
            TempData["ErrorMessage"] = "No se pudo cargar el listado de usuarios.";
            ActiveUsers = [];
        }

        DeactivateUsername = ResolveUsername(DeactivateUserId);

        var employeesResult = await usersApiAdapter.GetAvailableEmployeesAsync(HttpContext.RequestAborted);
        if (employeesResult.Success && employeesResult.Data != null)
        {
            AvailableEmployees = employeesResult.Data;
            return;
        }

        logger.LogWarning("No se pudo cargar el listado de empleados disponibles: {Errors}", string.Join(" | ", employeesResult.Errors));
        TempData["ErrorMessage"] = "No se pudo cargar el listado de empleados disponibles.";
        AvailableEmployees = [];
    }

    private void ApplyTemporaryPasswordErrors(ApiResponseDto<bool> result)
    {
        if (result.ValidationErrors.Count == 0)
        {
            ApplyApiErrors(result, nameof(TemporaryPassword));
            return;
        }

        foreach (var error in result.ValidationErrors)
        {
            var key = error.Key switch
            {
                "Password" => $"{nameof(TemporaryPassword)}.{nameof(TemporaryPassword.TemporaryPassword)}",
                "ConfirmPassword" => $"{nameof(TemporaryPassword)}.{nameof(TemporaryPassword.ConfirmTemporaryPassword)}",
                _ => $"{nameof(TemporaryPassword)}.{error.Key}"
            };

            foreach (var message in error.Value)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    ModelState.AddModelError(key, message);
                }
            }
        }
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

        return ActiveUsers.FirstOrDefault(user => user.Id == userId)?.UserName ?? string.Empty;
    }

    private static bool NeedsDeactivateModal(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        return !errorMessage.Contains("propio usuario", StringComparison.OrdinalIgnoreCase)
            && !errorMessage.Contains("administrador", StringComparison.OrdinalIgnoreCase);
    }
}
