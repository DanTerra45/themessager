namespace Mercadito.src.domain.users.entities
{
    public sealed class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }
        public long? EmployeeId { get; set; }
        public UserRole Role { get; set; } = UserRole.Operador;
        public bool MustChangePassword { get; set; }
        public string State { get; set; } = "A";
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
