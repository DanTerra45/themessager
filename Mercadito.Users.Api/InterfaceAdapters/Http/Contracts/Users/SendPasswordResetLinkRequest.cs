namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record SendPasswordResetLinkRequest(
    string ResetUrlBase);
