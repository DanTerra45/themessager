namespace Mercadito.Frontend.Dtos.Users;

public sealed record UserSummaryDto(
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
    DateTime? LastLogin)
{
    public string? EmployeeFullName => EmployeeName;
}
