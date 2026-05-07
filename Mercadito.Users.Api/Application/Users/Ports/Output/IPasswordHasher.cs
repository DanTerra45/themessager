namespace Mercadito.Users.Api.Application.Users.Ports.Output
{
    public interface IPasswordHasher
    {
        string Hash(string plainTextPassword);
    }
}
