using System.Security.Claims;
using Mercadito.src.domain.audit.entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mercadito.src.domain.shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Globalization;

namespace Mercadito.Pages.Infrastructure
{
    public abstract class AppPageModel : PageModel
    {
        protected AuditActor BuildAuditActor()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = 0L;

            if (!string.IsNullOrWhiteSpace(userIdValue) && long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var username = string.Empty;
            if (User.Identity?.Name != null)
            {
                username = User.Identity.Name;
            }

            return new AuditActor
            {
                UserId = userId,
                Username = username,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
        }

        protected void ApplyResultErrors(Result result, string prefix = "")
        {
            ArgumentNullException.ThrowIfNull(result);

            ApplyValidationErrors(prefix, result.Errors);

            if (result.Errors.Count == 0 && !string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
            }
        }

        protected void ApplyValidationErrors(string prefix, IReadOnlyDictionary<string, List<string>> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            foreach (var error in errors)
            {
                var key = string.Empty;
                if (!string.IsNullOrWhiteSpace(error.Key))
                {
                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        key = error.Key;
                    }
                    else
                    {
                        key = string.Concat(prefix, ".", error.Key);
                    }
                }

                foreach (var message in error.Value)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        ModelState.AddModelError(key, message);
                    }
                }
            }
        }

        protected void StoreModelStateErrors(string sessionKey)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(GetModelErrorMessage)
                        .ToArray());

            if (errors.Count == 0)
            {
                HttpContext.Session.Remove(sessionKey);
                return;
            }

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(errors));
        }

        protected void RestoreModelStateErrors(string sessionKey, ILogger logger)
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

                foreach (var error in errors)
                {
                    if (error.Value == null)
                    {
                        continue;
                    }

                    foreach (var message in error.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                }
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "No se pudo restaurar errores de validación para key {SessionKey}", sessionKey);
            }
        }

        protected Uri BuildAbsolutePathUrl(string relativePath)
        {
            ArgumentNullException.ThrowIfNull(relativePath);

            var pathBase = Request.PathBase.Value;
            if (string.IsNullOrWhiteSpace(pathBase))
            {
                pathBase = string.Empty;
            }

            var normalizedPath = relativePath;
            if (!normalizedPath.StartsWith('/'))
            {
                normalizedPath = "/" + normalizedPath;
            }

            return new Uri($"{Request.Scheme}://{Request.Host}{pathBase}{normalizedPath}", UriKind.Absolute);
        }

        private static string GetModelErrorMessage(ModelError error)
        {
            if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
            {
                return error.ErrorMessage;
            }

            return "Valor inválido.";
        }
    }
}
