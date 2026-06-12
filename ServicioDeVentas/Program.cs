using Application.Factory;
using Application.Options;
using Application.Service;
using Application.UseCases;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Domain.Events;
using Domain.Repository;
using Infrastructure.Database;
using Infrastructure.Messaging;
using Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddScoped<GetSaleByUseCase>();
builder.Services.AddScoped<GetAllSalesUseCase>();
builder.Services.AddScoped<GetSalesReportUseCase>();

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
