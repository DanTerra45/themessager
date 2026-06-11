using Application.Factory;
using Application.Options;
using Application.UseCases;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Database;
using Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<SaleRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<ICrudRepository<SaleWithDetails, int, SaleFields, SaleOptions>, SaleRepository>();
builder.Services.AddScoped<ICrudRepository<Customer, int, CustomerFields, CustomerOptions>, CustomerRepository>();
builder.Services.AddScoped<IRepositoryFactory<SaleWithDetails, int, SaleFields, SaleOptions>, SaleFactory>();
builder.Services.AddScoped<GetAllSalesUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var httpsPortConfigured = !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"])
    || !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"]);

if (httpsPortConfigured)
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    service = "ServicioVentas",
    status = "Healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();
