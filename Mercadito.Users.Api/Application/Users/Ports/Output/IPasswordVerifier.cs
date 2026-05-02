namespace Mercadito.Users.Api.Application.Users.Ports.Output
{
    public interface IPasswordVerifier
    {
        bool Verify(string plainTextPassword, string passwordHash);
    }
}
