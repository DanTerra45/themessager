namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record RegisterUserRequest(
    string Email,
    long? EmployeeId,
    string Role,
    string SetupUrlBase);
