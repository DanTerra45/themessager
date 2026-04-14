using System.Security.Claims;
using System.Globalization;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.application.users;
using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel(IAuthenticateUserUseCase authenticateUserUseCase) : AppPageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = NormalizeReturnUrl(returnUrl);
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(ReturnUrl);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ReturnUrl = NormalizeReturnUrl(ReturnUrl);

            var result = await authenticateUserUseCase.ExecuteAsync(new LoginUserCommand
            {
                Username = Username,
                Password = Password
            }, HttpContext.RequestAborted);

            if (result.IsFailure)
            {
                ApplyResultErrors(result);

                if (result.Errors.Count == 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                return Page();
            }

            var user = result.Value;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (user.MustChangePassword)
            {
                claims.Add(new Claim(UserClaimTypes.MustChangePassword, "true"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            TempData["SuccessMessage"] = "Sesión iniciada.";
            return LocalRedirect(ReturnUrl);
        }

        public async Task<IActionResult> OnPostSignOutAsync(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Sesión cerrada.";
            return LocalRedirect(NormalizeReturnUrl(returnUrl));
        }

        private static string NormalizeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return "/";
            }

            if (returnUrl.StartsWith('/'))
            {
                return returnUrl;
            }

            return "/";
        }
    }
}
