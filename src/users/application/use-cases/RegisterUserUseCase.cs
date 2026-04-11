using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.notifications.application.models;
using Mercadito.src.notifications.application.exceptions;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Mercadito.src.users.application.emails;
using Mercadito.src.shared.domain;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.users.application.usecases
{
    public sealed class RegisterUserUseCase(
        IUserRepository userRepository,
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IPasswordHasher passwordHasher,
        ICreateUserValidator validator,
        IEmailSender emailSender,
        IAuditTrailService auditTrailService,
        ILogger<RegisterUserUseCase> logger) : IRegisterUserUseCase
    {
        public async Task<Result<long>> ExecuteAsync(CreateUserDto user, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<long>(actorValidation.ErrorMessage);
            }

            var validationResult = validator.Validate(user);
            if (validationResult.IsFailure)
            {
                return Result.Failure<long>(validationResult.Errors);
            }

            try
            {
                var normalized = validationResult.Value;
                emailSender.EnsureAvailable();
                var generatedUsername = await userRepository.GenerateUniqueUsernameAsync(normalized.Email, cancellationToken);
                var generatedPasswordHash = passwordHasher.Hash(PasswordResetTokenCodec.CreatePlainToken());
                var plainToken = PasswordResetTokenCodec.CreatePlainToken();
                var tokenHash = PasswordResetTokenCodec.HashToken(plainToken);
                var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

                var userToCreate = new CreateUserDto
                {
                    Username = generatedUsername,
                    Email = normalized.Email,
                    EmployeeId = normalized.EmployeeId,
                    Role = normalized.Role,
                    SetupUrlBase = normalized.SetupUrlBase
                };

                var setupLink = BuildSetupLink(userToCreate.SetupUrlBase, plainToken);
                var onboardingEmail = BuildOnboardingEmail(userToCreate, setupLink);

                var userId = await userAccessWorkflowRepository.CreateWithOnboardingAsync(
                    userToCreate,
                    generatedPasswordHash,
                    tokenHash,
                    expiresAtUtc,
                    onboardingEmail,
                    cancellationToken);

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Create,
                    "usuarios",
                    userId,
                    null,
                    new
                    {
                        userToCreate.Username,
                        userToCreate.Email,
                        userToCreate.EmployeeId,
                        userToCreate.Role
                    },
                    cancellationToken);
                return Result.Success(userId);
            }
            catch (EmailDeliveryException deliveryException)
            {
                logger.LogWarning(deliveryException, "No se pudo completar el alta por correo para el usuario {Email}.", user.Email);
                return Result.Failure<long>("El usuario se registró, pero no se pudo enviar el correo de activación. Revisa SMTP y usa el restablecimiento para reenviar acceso.");
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<long>(validationException.Errors);
                }

                return Result.Failure<long>(validationException.Message);
            }
        }

        private static EmailMessage BuildOnboardingEmail(CreateUserDto user, string setupLink)
        {
            return new EmailMessage
            {
                ToAddress = user.Email,
                Subject = "Activa tu acceso a Mercadito",
                PlainTextBody =
                    $"Hola {user.Username},\n\n" +
                    "Se creó tu acceso al sistema Mercadito.\n" +
                    $"Rol asignado: {user.Role}.\n" +
                    "Abre el siguiente enlace para definir tu contraseña:\n" +
                    $"{setupLink}\n\n" +
                    "El enlace vence en 30 minutos.\n",
                HtmlBody = UserAccessEmailTemplate.BuildOnboardingHtml(user.Username, user.Role.ToString(), setupLink)
            };
        }

        private static string BuildSetupLink(string resetUrlBase, string token)
        {
            var separator = "/";
            if (resetUrlBase.EndsWith('/'))
            {
                separator = string.Empty;
            }

            return $"{resetUrlBase}{separator}{Uri.EscapeDataString(token)}";
        }
    }
}
