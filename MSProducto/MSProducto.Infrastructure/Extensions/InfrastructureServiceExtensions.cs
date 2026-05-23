namespace MSProducto.Infrastructure.Extensions
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using MSProducto.Domain.Ports.Output;
    using MSProducto.Infrastructure.Persistence;
    using MSProducto.Application.Services;

    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            var cs = config.GetConnectionString("MySqlConnection")!;
            services.AddSingleton<IDbConnectionFactory>(_ => new MySqlConnectionFactory(cs));
            services.AddScoped<IProductoRepository, MySqlProductoRepository>();
            services.AddScoped<ICategoriaRepository, MySqlCategoriaRepository>();
            services.AddScoped<ICookieUserExtractor, HeaderUserExtractor>();
            return services;
        }
    }
}