namespace Mercadito.Frontend.Services;

using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;

    public AuthHeaderHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var user = _ctx.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = user.Identity.Name;

            if (!string.IsNullOrWhiteSpace(userId))
                request.Headers.TryAddWithoutValidation("X-User-Id", userId);

            if (!string.IsNullOrWhiteSpace(username))
                request.Headers.TryAddWithoutValidation("X-Username", username);
        }

        return base.SendAsync(request, ct);
    }
}