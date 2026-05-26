namespace Mercadito.Frontend.Dtos.Users;

public sealed class AssignTemporaryPasswordRequestDto
{
    public long UserId { get; set; }

    public string Username { get; set; } = string.Empty;
}
