using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;

namespace Application.UseCases
{
    public class AssignTemporaryPasswordUseCase
    {
        private readonly UserService _userService;
        private readonly ILogger<AssignTemporaryPasswordUseCase> _logger;
        private readonly EmailService _emailService;
        public AssignTemporaryPasswordUseCase(UserService userService, ILogger<AssignTemporaryPasswordUseCase> logger, EmailService emailService)
        {
            _userService = userService;
            _logger = logger;
            _emailService = emailService;
        }
        public async Task<Result<bool>> Execute(int userId)
        {
            var (password, tempPassword) = PasswordUtils.GenerateSecurePassword(20);
            var userOptions = new UserOptions();
            userOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userId);
            userOptions.SelectFields([UserFields.Email, UserFields.Username, UserFields.Role]);
            var user = await _userService.GetOneAsync(userOptions);
            _logger.LogInformation("Assigning temporary password to user {UserId}", userId);
            user.Value.Password = tempPassword;
            user.Value.NeedPasswordChange = true;
            var result = await _userService.UpdateAsync<User>(user.Value, userOptions);
            userOptions = new UserOptions();
            userOptions.AddFilter(UserFields.Id, FilterOperator.Equals, userId);
            userOptions.SelectFields([UserFields.Password, UserFields.NeedPasswordChange]);
            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Errors);
            }
            await _emailService.SendOnboardingAsync(
                user.Value.Email,
                user.Value.Username ,
                user.Value.Role.ToString(),
                password,
                "http://localhost:5173/define-password"
            );
            return Result<bool>.Success(true);
        }
    }
}