namespace Mercadito.src.application.users.ports.output
{
    public interface IPasswordVerifier
    {
        bool Verify(string plainTextPassword, string passwordHash);
    }
}
