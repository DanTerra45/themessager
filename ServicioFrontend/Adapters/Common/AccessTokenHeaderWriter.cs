using System.Net.Http.Headers;
using System.Security.Claims;
using ServicioFrontend.Authentication;

namespace ServicioFrontend.Adapters.Common;

internal static class AccessTokenHeaderWriter
{
    public static void Apply(HttpRequestMessage message, IHttpContextAccessor httpContextAccessor)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue(FrontendUserClaimTypes.AccessToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("Cookie", $"access_token={token}");
    }
}
