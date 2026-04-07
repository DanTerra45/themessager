namespace Mercadito.src.users.application.ports.output
{
    public interface IPasswordVerifier
    {
        bool Verify(string plainTextPassword, string passwordHash);
    }
}
