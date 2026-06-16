using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServicioReportes.Application.Common.Ports;
using ServicioReportes.Application.Reports.Ports.Input;
using ServicioReportes.Application.Reports.Ports.Output;
using ServicioReportes.Application.Reports.UseCases;
using ServicioReportes.Authentication;
using ServicioReportes.Contracts.Common;
using ServicioReportes.Infrastructure.Messaging;
using ServicioReportes.Infrastructure.Reports.Persistence;
using ServicioReportes.Infrastructure.Shared.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

DapperTypeHandlers.Register();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IReportsDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<IReportsReadRepository, ReportsReadRepository>();
builder.Services.AddScoped<IReportsReadUseCase, ReportsReadUseCase>();
builder.Services.AddScoped<SalesReportProjectionWriter>();
builder.Services.AddHostedService<SalesReportingEventConsumer>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions is null || string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience) || string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("Falta configuracion JWT. Define Jwt:Issuer, Jwt:Audience y Jwt:Key con dotnet user-secrets para ServicioReportes.");
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
    options.AddPolicy("ReportsViewer", policy => policy.RequireRole("Admin", "Operator", "Operador", "Auditor"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var httpsPortConfigured = !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]) || !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"]);
if (httpsPortConfigured)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new ServiceHealthResponse("ServicioReportes", "Healthy", DateTimeOffset.UtcNow)));
app.Run();
