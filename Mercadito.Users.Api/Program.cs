using Mercadito.Users.Api.Application.Audit.Ports.Input;
using Mercadito.Users.Api.Application.Audit.Ports.Output;
using Mercadito.Users.Api.Application.Audit.Services;
using Mercadito.Users.Api.Application.Audit.UseCases;
using Mercadito.Users.Api.Application.Notifications.Ports.Output;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Application.Users.UseCases;
using Mercadito.Users.Api.Application.Users.Validation;
using Mercadito.Users.Api.Infrastructure.Audit.Persistence;
using Mercadito.Users.Api.Infrastructure.Notifications.Background;
using Mercadito.Users.Api.Infrastructure.Notifications.Email;
using Mercadito.Users.Api.Infrastructure.Notifications.Options;
using Mercadito.Users.Api.Infrastructure.Notifications.Persistence;
using Mercadito.Users.Api.Infrastructure.Users.Persistence;
using Mercadito.Users.Api.Infrastructure.Users.Security;
using Mercadito.Users.Api.Infrastructure.Shared.Persistence;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection("EmailOutbox"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddHostedService<EmailOutboxDispatcher>();

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IUserAccessWorkflowRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
builder.Services.AddScoped<EmailOutboxRepository>();
builder.Services.AddScoped<IEmailOutboxRepository>(serviceProvider => serviceProvider.GetRequiredService<EmailOutboxRepository>());
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<IAuditRepository>(serviceProvider => serviceProvider.GetRequiredService<AuditRepository>());
builder.Services.AddScoped<IRegisterAuditEntryUseCase, RegisterAuditEntryUseCase>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();

builder.Services.AddSingleton<SodiumPasswordService>();
builder.Services.AddSingleton<IPasswordHasher>(serviceProvider => serviceProvider.GetRequiredService<SodiumPasswordService>());
builder.Services.AddSingleton<IPasswordVerifier>(serviceProvider => serviceProvider.GetRequiredService<SodiumPasswordService>());

builder.Services.AddTransient<ICreateUserValidator, CreateUserValidator>();
builder.Services.AddTransient<IAssignTemporaryPasswordValidator, AssignTemporaryPasswordValidator>();
builder.Services.AddTransient<ISendAdministrativePasswordResetLinkValidator, SendAdministrativePasswordResetLinkValidator>();
builder.Services.AddTransient<IForcePasswordChangeValidator, ForcePasswordChangeValidator>();
builder.Services.AddTransient<ILoginUserValidator, LoginUserValidator>();
builder.Services.AddTransient<IRequestPasswordResetValidator, RequestPasswordResetValidator>();
builder.Services.AddTransient<ICompletePasswordResetValidator, CompletePasswordResetValidator>();

builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<IRequestPasswordResetUseCase, RequestPasswordResetUseCase>();
builder.Services.AddScoped<IValidatePasswordResetTokenUseCase, ValidatePasswordResetTokenUseCase>();
builder.Services.AddScoped<ICompletePasswordResetUseCase, CompletePasswordResetUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ISendAdministrativePasswordResetLinkUseCase, SendAdministrativePasswordResetLinkUseCase>();
builder.Services.AddScoped<IAssignTemporaryPasswordUseCase, AssignTemporaryPasswordUseCase>();
builder.Services.AddScoped<IForcePasswordChangeUseCase, ForcePasswordChangeUseCase>();
builder.Services.AddScoped<IDeactivateUserUseCase, DeactivateUserUseCase>();
builder.Services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();
builder.Services.AddScoped<IGetAvailableEmployeesUseCase, GetAvailableEmployeesUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new ServiceHealthResponse(
        "Mercadito.Users.Api",
        "Healthy",
        DateTimeOffset.UtcNow)));

app.MapControllers();

app.Run();
