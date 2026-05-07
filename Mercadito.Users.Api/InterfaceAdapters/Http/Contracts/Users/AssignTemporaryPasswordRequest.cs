namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record AssignTemporaryPasswordRequest(
    string TemporaryPassword,
    string ConfirmTemporaryPassword);
