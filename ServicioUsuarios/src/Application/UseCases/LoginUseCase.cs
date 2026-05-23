using Application.Auth;
using Application.Options;
using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Database;
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

        public async Task<Result<LoginResponse>> Execute(UserLoginRequest loginRequest)
        {
            var options = new UserOptions();
            options.AddFilter(UserFields.Email, FilterOperator.Equals, loginRequest.Email);
            var userResult = await _userService.GetOneAsync(options);

            if (!userResult.IsSuccess )
            {
                _logger.LogWarning("Login failed for email: {Email}", loginRequest.Email);
                return Result<LoginResponse>.Failure(new AppError("401", "Invalid credentials", ErrorType.Conflict));
            }
            _logger.LogInformation("Login successful for email: {Email}", loginRequest.Email);
            var authenticated = PasswordUtils.VerifyPassword(loginRequest.Password, userResult.Value.Password);
            if (!authenticated) 
            {
                _logger.LogWarning("Login failed for email: {Email}", loginRequest.Email);
                return Result<LoginResponse>.Failure(new AppError("401", "Invalid credentials", ErrorType.Conflict));
            }
            var token = _jwtService.GenerateToken(userResult.Value.ToJwtPayload());
            userResult.Value.LastLogin = DateTime.UtcNow;
            options = new UserOptions();
            options.AddFilter(UserFields.Id, FilterOperator.Equals, userResult.Value.Id);
            options.SelectFields([UserFields.LastLogin]);
            await _userService.UpdateAsync(userResult.Value, options);
            return Result<LoginResponse>.Success(new LoginResponse(token, userResult.Value.NeedPasswordChange));
        }
    }
}