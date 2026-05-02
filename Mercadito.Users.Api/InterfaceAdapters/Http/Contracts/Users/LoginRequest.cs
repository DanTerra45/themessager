namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record LoginRequest(
    string UserName,
    string Password);
