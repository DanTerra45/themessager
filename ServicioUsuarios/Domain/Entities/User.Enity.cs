using Domain.Entities.Enums;

namespace Domain.Entities;

public class User
{
    public int? Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int CreatorId { get; set; } 
    public DateTime? LastLogin { get; set; }
    public bool NeedPasswordChange { get; set; }
    public UserState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"User(Id={Id}, Username={Username}, Email={Email}, Role={Role}, State={State})";
    }
}
