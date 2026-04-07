using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.notifications.application.models;
using Mercadito.src.notifications.application.exceptions;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICreateUserValidator _validator;
        private readonly IEmailSender _emailSender;
        private readonly IAuditTrailService _auditTrailService;
        private readonly ILogger<RegisterUserUseCase> _logger;

        public RegisterUserUseCase(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ICreateUserValidator validator,
            IEmailSender emailSender,
            IAuditTrailService auditTrailService,
            ILogger<RegisterUserUseCase> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _validator = validator;
            _emailSender = emailSender;
            _auditTrailService = auditTrailService;
            _logger = logger;
        }

        public async Task<Result<long>> ExecuteAsync(CreateUserDto user, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result<long>.Failure(actorValidation.ErrorMessage);
            }

            var validationResult = _validator.Validate(user);
            if (validationResult.IsFailure)
            {
                return Result<long>.Failure(validationResult.Errors);
            }

            try
            {
                var normalized = validationResult.Value;
                _emailSender.EnsureAvailable();
                var generatedUsername = await _userRepository.GenerateUniqueUsernameAsync(normalized.Email, cancellationToken);
                var generatedPasswordHash = _passwordHasher.Hash(PasswordResetTokenCodec.CreatePlainToken());
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

                var userId = await _userRepository.CreateWithOnboardingAsync(
                    userToCreate,
                    generatedPasswordHash,
                    tokenHash,
                    expiresAtUtc,
                    onboardingEmail,
                    cancellationToken);

                await _auditTrailService.RecordAsync(
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
                return Result<long>.Success(userId);
            }
            catch (EmailDeliveryException deliveryException)
            {
                _logger.LogWarning(deliveryException, "No se pudo completar el alta por correo para el usuario {Email}.", user.Email);
                return Result<long>.Failure("El usuario se registró, pero no se pudo enviar el correo de activación. Revisa SMTP y usa el restablecimiento para reenviar acceso.");
            }
            catch (BusinessValidationException validationException)
            {
                return validationException.Errors.Count > 0
                    ? Result<long>.Failure(validationException.Errors)
                    : Result<long>.Failure(validationException.Message);
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
                HtmlBody =
                    $"<p>Hola <strong>{user.Username}</strong>,</p>" +
                    "<p>Se creó tu acceso al sistema Mercadito.</p>" +
                    $"<p><strong>Rol asignado:</strong> {user.Role}</p>" +
                    $"<p><a href=\"{setupLink}\">Haz clic aquí para definir tu contraseña</a></p>" +
                    "<p>El enlace vence en 30 minutos.</p>"
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
