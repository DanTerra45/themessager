namespace Mercadito.src.application.users.ports.output
{
    public interface IPasswordHasher
    {
        string Hash(string plainTextPassword);
    }
}
