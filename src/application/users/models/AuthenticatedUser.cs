using Mercadito.src.domain.users.entities;

namespace Mercadito.src.application.users.models
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
