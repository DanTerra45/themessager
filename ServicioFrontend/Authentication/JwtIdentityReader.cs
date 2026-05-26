using System.Text;
using System.Text.Json;

namespace ServicioFrontend.Authentication;

public sealed record JwtIdentity(
    long UserId,
    string Username,
    string Role);

public static class JwtIdentityReader
{
    public static bool TryRead(string token, out JwtIdentity? identity)
    {
        identity = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var payloadBytes = DecodeBase64Url(parts[1]);
            using var payloadDocument = JsonDocument.Parse(payloadBytes);
            var payload = payloadDocument.RootElement;

            var subject = GetString(payload, "sub");
            var username = GetString(payload, "nickname")
                ?? GetString(payload, "unique_name")
                ?? GetString(payload, "email")
                ?? string.Empty;
            var role = GetString(payload, "role") ?? string.Empty;

            if (!long.TryParse(subject, out var userId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            identity = new JwtIdentity(userId, username, role);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4)
        {
            base64 = base64.PadRight(base64.Length + padding, '=');
        }

        return Convert.FromBase64String(base64);
    }

    private static string? GetString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : element.ToString();
    }
}
