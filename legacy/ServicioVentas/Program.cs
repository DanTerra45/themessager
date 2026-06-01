using Mercadito.Sales.Api.Application.Audit.Ports.Input;
using Mercadito.Sales.Api.Application.Audit.Ports.Output;
using Mercadito.Sales.Api.Application.Audit.Services;
using Mercadito.Sales.Api.Application.Audit.UseCases;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Application.Sales.Facades;
using Mercadito.Sales.Api.Application.Sales.Ports.Input;
using Mercadito.Sales.Api.Application.Sales.Ports.Output;
using Mercadito.Sales.Api.Application.Sales.Validation;
using Mercadito.Sales.Api.Domain.Shared.Validation;
using Mercadito.Sales.Api.Infrastructure.Audit.Persistence;
using Mercadito.Sales.Api.Infrastructure.Sales.Persistence;
using Mercadito.Sales.Api.Infrastructure.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IRegisterAuditEntryUseCase, RegisterAuditEntryUseCase>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();
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
