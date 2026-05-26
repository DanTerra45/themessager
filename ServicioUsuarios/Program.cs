using Application.Auth;
using Application.Factories;
using Application.Options;
using Application.Service;
using Application.UseCases;
using Application.Utils;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Database;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtService>();

var postgresConnection = builder.Configuration.GetConnectionString("PostgresConnection");
if (string.IsNullOrWhiteSpace(postgresConnection))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:PostgresConnection. Configuralo con dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set \"ConnectionStrings:PostgresConnection\" \"<valor>\".");
}

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions is null
    || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Falta configuracion JWT. Define Jwt:Issuer, Jwt:Audience y Jwt:Key con dotnet user-secrets para ServicioUsuarios.");
}

if (jwtOptions.ExpiresMinutes <= 0)
{
    throw new InvalidOperationException("Jwt:ExpiresMinutes debe ser mayor a 0.");
}

var key = Encoding.UTF8.GetBytes(jwtOptions.Key);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // para usar los nombres de claim personalizados
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
                // Lee el JWT desde cookie en vez de Authorization header
                if (context.Request.Cookies.TryGetValue("access_token", out var token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("OperatorOrAdmin", p => p.RequireRole("Operator", "Admin"));
});

builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserStoryRepository>();
builder.Services.AddScoped<PasswordResetTokenRepository>();

builder.Services.AddScoped<IRepositoryFactory<User,int,UserFields,UserOptions>, UserFactory>();
builder.Services.AddScoped<IRepositoryFactory<UserStory,int,UserStoryFields,UserStoryOptions>, UserStoryFactory>();
builder.Services.AddScoped<IRepositoryFactory<PasswordResetToken,int,PasswordResetTokenFields,PasswordResetTokenOptions>, PasswordResetTokenFactory>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserStoryService>();
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<ChangePasswordUseCase>();
builder.Services.AddScoped<PasswordResetTokenService>();
builder.Services.AddScoped<RequestPasswordResetUseCase>();
builder.Services.AddScoped<ResetPasswordUseCase>();
builder.Services.AddScoped<ForgotPasswordUseCase>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<MailKitEmailSender>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AssignTemporaryPasswordUseCase>();
builder.Services.AddScoped<DisableUserUseCase>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        service = "ServicioUsuarios",
        status = "Healthy",
        timestamp = DateTimeOffset.UtcNow
    }));

app.Run();

