using Mercadito.Sales.Api.Application.Audit.Ports.Input;
using Mercadito.Sales.Api.Application.Audit.Ports.Output;
using Mercadito.Sales.Api.Application.Audit.Services;
using Mercadito.Sales.Api.Application.Audit.UseCases;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Application.Categories.Ports.Input;
using Mercadito.Sales.Api.Application.Categories.Ports.Output;
using Mercadito.Sales.Api.Application.Categories.UseCases;
using Mercadito.Sales.Api.Application.Categories.Validation;
using Mercadito.Sales.Api.Application.Employees.Ports.Input;
using Mercadito.Sales.Api.Application.Employees.Ports.Output;
using Mercadito.Sales.Api.Application.Employees.UseCases;
using Mercadito.Sales.Api.Application.Employees.Validation;
using Mercadito.Sales.Api.Application.Products.Ports.Input;
using Mercadito.Sales.Api.Application.Products.Ports.Output;
using Mercadito.Sales.Api.Application.Products.UseCases;
using Mercadito.Sales.Api.Application.Products.Validation;
using Mercadito.Sales.Api.Application.Sales.Facades;
using Mercadito.Sales.Api.Application.Sales.Ports.Input;
using Mercadito.Sales.Api.Application.Sales.Ports.Output;
using Mercadito.Sales.Api.Application.Sales.Validation;
using Mercadito.Sales.Api.Domain.Shared.Validation;
using Mercadito.Sales.Api.Domain.Categories.Factories;
using Mercadito.Sales.Api.Domain.Employees.Factories;
using Mercadito.Sales.Api.Domain.Products.Factories;
using Mercadito.Sales.Api.Infrastructure.Audit.Persistence;
using Mercadito.Sales.Api.Infrastructure.Categories.Persistence;
using Mercadito.Sales.Api.Infrastructure.Employees.Persistence;
using Mercadito.Sales.Api.Infrastructure.Products.Persistence;
using Mercadito.Sales.Api.Infrastructure.Sales.Persistence;
using Mercadito.Sales.Api.Infrastructure.Suppliers.Persistence;
using Mercadito.Sales.Api.Infrastructure.Shared.Persistence;
using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Application.Suppliers.UseCases;
using Mercadito.Sales.Api.Application.Suppliers.Validation;

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
builder.Services.AddScoped<IEmployeeFactory, EmployeeFactory>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ICreateEmployeeValidator, CreateEmployeeValidator>();
builder.Services.AddScoped<IUpdateEmployeeValidator, UpdateEmployeeValidator>();
builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
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
builder.Services.AddScoped<ISalesRepository, SalesRepository>();
builder.Services.AddScoped<ISalesQueryFacade, SalesQueryFacade>();
builder.Services.AddScoped<IRegisterSaleValidator, RegisterSaleValidator>();
builder.Services.AddScoped<ICancelSaleValidator, CancelSaleValidator>();
builder.Services.AddScoped<IRegisterSaleFacade, RegisterSaleFacade>();
builder.Services.AddScoped<ICancelSaleFacade, CancelSaleFacade>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new ServiceHealthResponse(
        "Mercadito.Sales.Api",
        "Healthy",
        DateTimeOffset.UtcNow)));

app.MapControllers();

app.Run();
