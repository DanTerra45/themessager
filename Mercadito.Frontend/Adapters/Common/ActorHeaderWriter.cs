using System.Globalization;
using Mercadito.Frontend.Dtos.Common;

namespace Mercadito.Frontend.Adapters.Common;

internal static class ActorHeaderWriter
{
    public static void Apply(HttpRequestMessage message, ApiActorContextDto? actor)
    {
        if (actor == null)
        {
            return;
        }

        if (actor.UserId > 0)
        {
            message.Headers.TryAddWithoutValidation(
                "X-User-Id",
                actor.UserId.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(actor.Username))
        {
            message.Headers.TryAddWithoutValidation("X-Username", actor.Username);
        }
    }
}
