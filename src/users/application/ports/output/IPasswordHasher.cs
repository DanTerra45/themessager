namespace Mercadito.src.users.application.ports.output
{
    public interface IPasswordHasher
    {
        string Hash(string plainTextPassword);
    }
}
