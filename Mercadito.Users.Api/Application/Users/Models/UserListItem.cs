using Mercadito.Users.Api.Domain.Users.Entities;

namespace Mercadito.Users.Api.Application.Users.Models
{
    public sealed class UserListItem
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public UserRole Role { get; set; } = UserRole.Operador;
        public string State { get; set; } = "A";
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeFullName { get; set; }
        public string? EmployeeCargo { get; set; }
    }
}
