namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;

public sealed record ServiceHealthResponse(
    string Service,
    string Status,
    DateTimeOffset CheckedAt);
