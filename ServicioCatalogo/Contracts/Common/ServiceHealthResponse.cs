namespace ServicioCatalogo.Contracts.Common;

public sealed record ServiceHealthResponse(
    string Service,
    string Status,
    DateTimeOffset CheckedAt);
