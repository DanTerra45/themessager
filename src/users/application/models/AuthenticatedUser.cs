using Mercadito.src.users.domain.entities;

namespace Mercadito.src.users.application.models
{
    public sealed class AuthenticatedUser
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Operador;
        public long? EmployeeId { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
