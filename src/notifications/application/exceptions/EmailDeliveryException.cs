namespace Mercadito.src.notifications.application.exceptions
{
    public sealed class EmailDeliveryException : Exception
    {
        public EmailDeliveryException(string message)
            : base(message)
        {
        }

        public EmailDeliveryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
