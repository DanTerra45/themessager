namespace Mercadito.src.shared.domain.exceptions
{
    public sealed class DataStoreUnavailableException : Exception
    {
        public DataStoreUnavailableException()
            : base("La base de datos no está disponible.")
        {
        }

        public DataStoreUnavailableException(string message)
            : base(message)
        {
        }

        public DataStoreUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
