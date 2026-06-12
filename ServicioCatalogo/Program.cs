using ServicioCatalogo.Application.Audit.Ports.Input;
using ServicioCatalogo.Application.Audit.Ports.Output;
using ServicioCatalogo.Application.Audit.Services;
using ServicioCatalogo.Application.Audit.UseCases;
using ServicioCatalogo.Application.Categories.Ports.Input;
using ServicioCatalogo.Application.Categories.Ports.Output;
using ServicioCatalogo.Application.Categories.UseCases;
using ServicioCatalogo.Application.Categories.Validation;
using ServicioCatalogo.Application.Products.Ports.Input;
using ServicioCatalogo.Application.Products.Ports.Output;
using ServicioCatalogo.Application.Products.UseCases;
using ServicioCatalogo.Application.Products.Validation;
using ServicioCatalogo.Contracts.Common;
using ServicioCatalogo.Domain.Categories.Factories;
using ServicioCatalogo.Domain.Products.Factories;
using ServicioCatalogo.Infrastructure.Audit.Persistence;
using ServicioCatalogo.Infrastructure.Categories.Persistence;
using ServicioCatalogo.Infrastructure.Messaging;
using ServicioCatalogo.Infrastructure.Products.Persistence;
using ServicioCatalogo.Infrastructure.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IRegisterAuditEntryUseCase, RegisterAuditEntryUseCase>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();

builder.Services.AddScoped<ICategoryFactory, CategoryFactory>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductCategoryLookupRepository, CategoryRepository>();
builder.Services.AddScoped<ICreateCategoryValidator, CreateCategoryValidator>();
builder.Services.AddScoped<IUpdateCategoryValidator, UpdateCategoryValidator>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();

builder.Services.AddScoped<IProductFactory, ProductFactory>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICreateProductValidator, CreateProductValidator>();
builder.Services.AddScoped<IUpdateProductValidator, UpdateProductValidator>();
builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddHostedService<StockSagaEventConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new ServiceHealthResponse(
        "ServicioCatalogo",
        "Healthy",
        DateTimeOffset.UtcNow)));

app.MapControllers();

app.Run();
