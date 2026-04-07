using System.Security.Claims;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : AppPageModel
    {
        private readonly IAuthenticateUserUseCase _authenticateUserUseCase;

        public LoginModel(IAuthenticateUserUseCase authenticateUserUseCase)
        {
            _authenticateUserUseCase = authenticateUserUseCase;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
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

            var result = await _authenticateUserUseCase.ExecuteAsync(new LoginUserCommand
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
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
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

            return returnUrl.StartsWith('/') ? returnUrl : "/";
        }
    }
}
