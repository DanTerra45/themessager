namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record RequestPasswordResetRequest(
    string Identifier,
    string ResetUrlBase);
