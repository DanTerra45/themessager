namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
