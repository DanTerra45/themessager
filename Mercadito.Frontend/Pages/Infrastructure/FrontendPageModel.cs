using System.Globalization;
using System.Security.Claims;
using Mercadito.Frontend.Dtos.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Mercadito.Frontend.Pages.Infrastructure;

public abstract class FrontendPageModel : PageModel
{
    protected ApiActorContextDto BuildActorContext()
    {
        var userId = 0L;
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdValue)
            && long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var username = User.Identity?.Name ?? string.Empty;
        return new ApiActorContextDto(userId, username);
    }

    protected long ResolveUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
        {
            return 0;
        }

        if (!long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            return 0;
        }

        return userId;
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

    protected void ApplyApiErrors<T>(ApiResponseDto<T> response, string prefix = "")
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.ValidationErrors.Count > 0)
        {
            ApplyValidationErrors(prefix, response.ValidationErrors);
            return;
        }

        foreach (var error in response.Errors)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }
    }

    protected void ApplyValidationErrors(string prefix, IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            var key = string.Empty;
            if (!string.IsNullOrWhiteSpace(error.Key))
            {
                key = string.IsNullOrWhiteSpace(prefix)
                    ? error.Key
                    : string.Concat(prefix, ".", error.Key);
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

    protected void RemoveModelStateForPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        var keysToRemove = ModelState.Keys
            .Where(key => IsModelStatePrefixMatch(key, prefix))
            .ToList();

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
    }

    protected bool IsModelStateValidForPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return ModelState.IsValid;
        }

        return ModelState
            .Where(entry => IsModelStatePrefixMatch(entry.Key, prefix))
            .All(entry => entry.Value?.Errors.Count == 0);
    }

    protected void LogInvalidModelState(ILogger logger, string context)
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var invalidEntries = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry =>
            {
                var messages = entry.Value!.Errors
                    .Select(GetModelErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message));

                return $"{entry.Key}: {string.Join(" | ", messages)}";
            })
            .ToList();

        logger.LogWarning(
            "ModelState inválido en {Context}: {InvalidEntries}",
            context,
            string.Join(" || ", invalidEntries));
    }

    protected string FirstErrorOrDefault<T>(ApiResponseDto<T> response, string fallback)
    {
        return response.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error)) ?? fallback;
    }

    protected static string GetModelErrorMessage(ModelError error)
    {
        if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
        {
            return error.ErrorMessage;
        }

        return "Valor inválido.";
    }

    private static bool IsModelStatePrefixMatch(string key, string prefix)
    {
        return string.Equals(key, prefix, StringComparison.OrdinalIgnoreCase)
            || key.StartsWith(string.Concat(prefix, "."), StringComparison.OrdinalIgnoreCase)
            || key.StartsWith(string.Concat(prefix, "["), StringComparison.OrdinalIgnoreCase);
    }
}
