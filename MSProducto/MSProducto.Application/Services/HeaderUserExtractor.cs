namespace MSProducto.Application.Services
{
    using Microsoft.AspNetCore.Http;

    public interface ICookieUserExtractor
    {
        string? GetCurrentUserId();
        string? GetCurrentUsername();
        bool IsAuthenticated();
    }

    public class HeaderUserExtractor : ICookieUserExtractor
    {
        private readonly IHttpContextAccessor _ctx;
        private const string UserIdHeader = "X-User-Id";
        private const string UsernameHeader = "X-Username";

        public HeaderUserExtractor(IHttpContextAccessor ctx) => _ctx = ctx;
        public string? GetCurrentUserId() => _ctx.HttpContext?.Request.Headers[UserIdHeader].FirstOrDefault();
        public string? GetCurrentUsername() => _ctx.HttpContext?.Request.Headers[UsernameHeader].FirstOrDefault();
        public bool IsAuthenticated() => !string.IsNullOrWhiteSpace(GetCurrentUserId())
                                        && !string.IsNullOrWhiteSpace(GetCurrentUsername());
    }
}