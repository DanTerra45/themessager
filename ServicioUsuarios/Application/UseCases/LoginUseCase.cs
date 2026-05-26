using Application.Auth;
using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Jwt;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Mappers;

namespace Application.UseCases
{
    public class LoginUseCase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;
        private readonly ILogger<LoginUseCase> _logger;
        public LoginUseCase(UserService userService, JwtService jwtService, ILogger<LoginUseCase> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
        }

        private async Task<Result<User>> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            var options = new UserOptions();
            options.AddLogicalFilter([new FilterCondition<UserFields>(UserFields.Email, FilterOperator.Equals, emailOrUsername),new FilterCondition<UserFields>(UserFields.Username, FilterOperator.Equals, emailOrUsername)], LogicalOperator.Or);

            var result = await _userService.GetOneAsync(options);
            if (!result.IsSuccess)            {
                _logger.LogWarning("No user found with email or username: {EmailOrUsername}", emailOrUsername);
                return Result<User>.Failure(new AppError("404", "User not found", ErrorType.NotFound));
            }
            return result;
        }

        public async Task<Result<LoginResponse>> Execute(UserLoginRequest loginRequest)
        {
            var userResult = await GetUserByEmailOrUsernameAsync(loginRequest.EmailOrUsername);
            if (!userResult.IsSuccess || userResult.Value is null)
            {
                return Result<LoginResponse>.Failure(new AppError("404", "User not found", ErrorType.NotFound));
            }
            var authenticated = PasswordUtils.VerifyPassword(loginRequest.Password, userResult.Value.Password);
            if (!authenticated)
            {
                _logger.LogWarning("Failed login attempt for user {EmailOrUsername}", loginRequest.EmailOrUsername);
                return Result<LoginResponse>.Failure(new AppError("401", "Invalid credentials", ErrorType.Unauthorized));
            }
            var token = _jwtService.GenerateToken(userResult.Value.ToJwtPayload());
            userResult.Value.LastLogin = DateTime.UtcNow;
            var options = new UserOptions();
            options.AddFilter(UserFields.Id, FilterOperator.Equals, userResult.Value.Id);
            options.SelectFields([UserFields.LastLogin]);
            await _userService.UpdateAsync(userResult.Value, options);
            return Result<LoginResponse>.Success(new LoginResponse(token, userResult.Value.NeedPasswordChange));
        }
    }
}
