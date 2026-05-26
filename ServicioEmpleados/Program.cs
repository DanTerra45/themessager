using ServicioEmpleados.Application.Employees.Ports.Input;
using ServicioEmpleados.Application.Employees.Ports.Output;
using ServicioEmpleados.Application.Employees.UseCases;
using ServicioEmpleados.Application.Employees.Validation;
using ServicioEmpleados.Contracts.Common;
using ServicioEmpleados.Domain.Employees.Factories;
using ServicioEmpleados.Infrastructure.Employees.Persistence;
using ServicioEmpleados.Infrastructure.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
builder.Services.AddScoped<IEmployeeFactory, EmployeeFactory>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ICreateEmployeeValidator, CreateEmployeeValidator>();
builder.Services.AddScoped<IUpdateEmployeeValidator, UpdateEmployeeValidator>();
builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new ServiceHealthResponse(
        "ServicioEmpleados",
        "Healthy",
        DateTimeOffset.UtcNow)));

app.MapControllers();

app.Run();
