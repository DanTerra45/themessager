using Mercadito.src.application.audit.ports.input;
using Mercadito.src.application.audit.ports.output;
using Mercadito.src.application.audit.services;
using Mercadito.src.application.audit.usecases;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.src.application.categories.ports.input;
using Mercadito.src.application.categories.ports.output;
using Mercadito.src.application.categories.usecases;
using Mercadito.src.application.categories.validation;
using Mercadito.src.application.employees.ports.input;
using Mercadito.src.application.employees.ports.output;
using Mercadito.src.application.employees.usecases;
using Mercadito.src.application.employees.validation;
using Mercadito.src.application.products.ports.input;
using Mercadito.src.application.products.ports.output;
using Mercadito.src.application.products.usecases;
using Mercadito.src.application.products.validation;
using Mercadito.src.application.sales.facades;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.application.sales.ports.output;
using Mercadito.src.application.sales.validation;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.domain.categories.factories;
using Mercadito.src.domain.employees.factories;
using Mercadito.src.domain.products.factories;
using Mercadito.src.infrastructure.audit.persistence;
using Mercadito.src.infrastructure.categories.persistence;
using Mercadito.src.infrastructure.employees.persistence;
using Mercadito.src.infrastructure.products.persistence;
using Mercadito.src.infrastructure.sales.persistence;
using Mercadito.src.infrastructure.suppliers.persistence;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.application.usecases;
using Mercadito.src.suppliers.application.validation;

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
