using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Application.Suppliers.UseCases;
using ServicioProveedores.Application.Suppliers.Validation;
using ServicioProveedores.Contracts.Common;
using ServicioProveedores.Domain.Suppliers.Factories;
using ServicioProveedores.Domain.Shared.Validation;
using ServicioProveedores.Infrastructure.Suppliers.Persistence;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configuralo con dotnet user-secrets para ServicioProveedores.");
}

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ISupplierFactory, SupplierFactory>();
builder.Services.AddScoped<CreateSupplierValidator>();
builder.Services.AddScoped<UpdateSupplierValidator>();
builder.Services.AddScoped<IValidator<CreateSupplierDto, SupplierDto>>(serviceProvider =>
    serviceProvider.GetRequiredService<CreateSupplierValidator>());
builder.Services.AddScoped<IValidator<UpdateSupplierDto, SupplierDto>>(serviceProvider =>
    serviceProvider.GetRequiredService<UpdateSupplierValidator>());
builder.Services.AddScoped<ISupplierFormHintsProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<CreateSupplierValidator>());
builder.Services.AddScoped<IGetAllSuppliersUseCase, GetAllSuppliersUseCase>();
builder.Services.AddScoped<IGetSupplierByIdUseCase, GetSupplierByIdUseCase>();
builder.Services.AddScoped<IGetNextSupplierCodeUseCase, GetNextSupplierCodeUseCase>();
builder.Services.AddScoped<IRegisterSupplierUseCase, RegisterSupplierUseCase>();
builder.Services.AddScoped<IUpdateSupplierUseCase, UpdateSupplierUseCase>();
builder.Services.AddScoped<IDeleteSupplierUseCase, DeleteSupplierUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new ServiceHealthResponse(
        "ServicioProveedores",
        "Healthy",
        DateTimeOffset.UtcNow)));

app.MapControllers();

app.Run();
