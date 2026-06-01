using System.Text.Json.Serialization;

namespace ServicioFrontend.Dtos.Users;

public sealed record UserSummaryDto(
    long Id,
    string Username,
    string Email,
    string Role,
    string State,
    bool NeedPasswordChange,
    DateTime? LastLogin,
    DateTime? CreatedAt)
{
    [JsonIgnore]
    public string UserName => Username;
    public long? EmployeeId => null;
    public string? EmployeeName => null;
    public string? EmployeeCargo => null;
    public string? EmployeeFullName => EmployeeName;
    public bool IsActive => string.Equals(State, "Active", StringComparison.OrdinalIgnoreCase);
    public DateTime? LastLoginAt => LastLogin;
}
