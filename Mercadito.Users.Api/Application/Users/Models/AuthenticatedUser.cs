using Mercadito.Users.Api.Domain.Users.Entities;

namespace Mercadito.Users.Api.Application.Users.Models
{
    public sealed class AuthenticatedUser
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Operador;
        public long? EmployeeId { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
