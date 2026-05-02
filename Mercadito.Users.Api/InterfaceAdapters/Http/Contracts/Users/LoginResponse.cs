namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record LoginResponse(
    long UserId,
    string UserName,
    string Role,
    long? EmployeeId,
    bool MustChangePassword,
    DateTime? LastLoginAt);
