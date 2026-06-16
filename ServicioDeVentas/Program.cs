using Application.Factory;
using Application.Options;
using Application.Sagas;
using Application.Service;
using Application.UseCases;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Domain.Events;
using Domain.Repository;
using Infrastructure.Database;
using Infrastructure.Integrations;
using Infrastructure.Messaging;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServicioVentas.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddSingleton<IStockSagaCoordinator, StockSagaCoordinator>();
builder.Services.AddHostedService<StockSagaResponseConsumer>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<SalesContractService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHttpClient<CatalogApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"] ?? "http://localhost:5150");
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions is null
    || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Falta configuracion JWT. Define Jwt:Issuer, Jwt:Audience y Jwt:Key con dotnet user-secrets para ServicioVentas.");
}

var key = Encoding.UTF8.GetBytes(jwtOptions.Key);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SalesViewer", policy => policy.RequireRole("Admin", "Operator", "Operador", "Auditor"));
    options.AddPolicy("SalesOperator", policy => policy.RequireRole("Admin", "Operator", "Operador"));
});

builder.Services.AddScoped<GetSaleByUseCase>();
builder.Services.AddScoped<GetAllSalesUseCase>();
builder.Services.AddScoped<GetSalesReportUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var httpsPortConfigured = !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"])
    || !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"]);

if (httpsPortConfigured)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    service = "ServicioVentas",
    status = "Healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();
