namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record ForcePasswordChangeRequest(
    string Password,
    string ConfirmPassword);
