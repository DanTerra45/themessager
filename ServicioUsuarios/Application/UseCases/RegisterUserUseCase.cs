using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Mappers;
using Domain.Database;
using Dapper;
using Domain.Database.Fields;
using Application.Options;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Events;
using Infrastructure.Database;

namespace Application.UseCases
{
    public class RegisterUserUseCase
    {
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterUserUseCase> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisher _eventPublisher;

        public RegisterUserUseCase(UserService userService, EmailService emailService, IUnitOfWork unitOfWork, ILogger<RegisterUserUseCase> logger, IConfiguration configuration, IEventPublisher eventPublisher)
        {
            _userService = userService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
        }

        public async Task<Result<int>> Execute(CreateUserDto createUserDto,int creatorId)
        {
            var (password, hashedPassword) = PasswordUtils.GenerateSecurePassword(20);
            var registerDto = createUserDto.ToRegisterDto(creatorId,hashedPassword);
            var userEntity = registerDto.ToEntity();

            try
            {
                await _unitOfWork.BeginAsync();

                var table = "users";
                var builder = new QueryBuilder<UserOptions, UserFields>(table, new UserSchema());
                var (sql, parameters) = builder.Insert<User>(new UserOptions(), userEntity).Build();

                var sqlWithReturning = sql + " RETURNING id";
                _logger.LogInformation("Executing SQL (transaction): {Sql}", sqlWithReturning);
                int newId = await _unitOfWork.Connection.QuerySingleAsync<int>(sqlWithReturning, parameters, _unitOfWork.Transaction);

                try
                {
                    var url = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
                    await _emailService.SendOnboardingAsync(
                        createUserDto.Email,
                        createUserDto.Username,
                        createUserDto.Role,
                        password,
                        url.TrimEnd('/') + "/login?email_or_username=" + createUserDto.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send onboarding email, rolling back transaction.");
                    await _unitOfWork.RollbackAsync();
                    return Result<int>.Failure(new AppError(emailEx.GetType().Name, emailEx.Message, ErrorType.Internal));
                }

                await _unitOfWork.CommitAsync();

                try
                {
                    await _eventPublisher.PublishAsync(
                        "users.created",
                        new UserCreatedEvent(
                            newId,
                            creatorId,
                            createUserDto.Username,
                            createUserDto.Email,
                            NormalizeRole(createUserDto.Role),
                            DateTime.UtcNow),
                        newId.ToString());
                }
                catch (Exception publishEx)
                {
                    _logger.LogError(publishEx, "El usuario {UserId} se creó, pero no se pudo publicar el evento users.created.", newId);
                }

                return Result<int>.Success(newId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration.");
                try { await _unitOfWork.RollbackAsync(); } catch { }
                return Result<int>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return UserRole.Operator.ToString();
            }

            return role.Trim().ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin.ToString(),
                "auditor" => UserRole.Auditor.ToString(),
                _ => UserRole.Operator.ToString()
            };
        }
    }
}
