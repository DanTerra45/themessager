namespace Mercadito.src.shared.domain.factory
{
    public abstract class RepositoryCreator<TRepository>
    {
        public abstract TRepository Create();
    }
}
