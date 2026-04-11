namespace Mercadito.Pages.Infrastructure
{
    public static class PaginationSettings
    {
        public static int ResolveDefaultPageSize(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            if (configuredPageSize > 0)
            {
                return configuredPageSize;
            }

            return 10;
        }
    }
}
