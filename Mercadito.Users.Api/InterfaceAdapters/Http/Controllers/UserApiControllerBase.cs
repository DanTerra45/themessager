using System.Security.Claims;
using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Users.Api.InterfaceAdapters.Http.Controllers;

public abstract class UserApiControllerBase : ControllerBase
{
    protected AuditActor BuildActor()
    {
        return new AuditActor
        {
            UserId = ResolveUserId(),
            Username = ResolveUsername(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
    }

    protected static ApiResponse<T> ToFailure<T>(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return ApiResponse<T>.Fail(result.Errors);
        }

        return ApiResponse<T>.Fail(GetErrors(result).ToArray());
    }

    private long ResolveUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdText, out var userId) && userId > 0)
        {
            return userId;
        }

        userIdText = Request.Headers["X-User-Id"].FirstOrDefault();
        if (long.TryParse(userIdText, out userId) && userId > 0)
        {
            return userId;
        }

        return 1;
    }

    private string ResolveUsername()
    {
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name;
        }

        var username = Request.Headers["X-Username"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return "frontend";
    }

    private static IReadOnlyList<string> GetErrors(Result result)
    {
        if (result.Errors.Count == 0)
        {
            return [result.ErrorMessage];
        }

        return result.Errors
            .SelectMany(error => error.Value)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .DefaultIfEmpty(result.ErrorMessage)
            .ToList();
    }
}
