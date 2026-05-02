namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record CompletePasswordResetRequest(
    string Token,
    string Password,
    string ConfirmPassword);
