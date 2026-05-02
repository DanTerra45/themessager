namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record UserSummaryResponse(
    long Id,
    string UserName,
    string Email,
    string Role,
    string State,
    long? EmployeeId,
    string? EmployeeName,
    string? EmployeeCargo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
