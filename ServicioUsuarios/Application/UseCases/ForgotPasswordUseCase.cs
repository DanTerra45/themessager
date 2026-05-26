using Application.Options;
using Application.Service;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Auth;
using Domain.Entities;
using Domain.Entities.Enums;

namespace Application.UseCases
{
    public class ForgotPasswordUseCase
    {
        private readonly UserService _userService;

        private readonly RequestPasswordResetUseCase _requestPasswordResetUseCase;
        private async Task<User?> FindUserByEmailOrUsername(ForgotPasswordRequest request)
        {
            var userOptions = new UserOptions();
            userOptions.AddFilter(UserFields.Email, FilterOperator.Equals, request.EmailOrUsername);
            var userResult = await _userService.GetOneAsync(userOptions);
            if (!userResult.IsSuccess || userResult.Value is null)
            {
                userOptions = new UserOptions();
                Console.WriteLine($"User not found with email: {request.EmailOrUsername}, trying username...");
                userOptions.AddFilter(UserFields.Username, FilterOperator.Equals, request.EmailOrUsername);
                userResult = await _userService.GetOneAsync(userOptions);
                Console.WriteLine($"User not found with username: {request.EmailOrUsername}");
                if (!userResult.IsSuccess || userResult.Value is null)
                {
                    return null;
                }
            }
            return userResult?.Value;
        }
        public ForgotPasswordUseCase(UserService userService, RequestPasswordResetUseCase requestPasswordResetUseCase)
        {
            _userService = userService;
            _requestPasswordResetUseCase = requestPasswordResetUseCase;
        }
        public async Task<Result<bool>> Execute(ForgotPasswordRequest request)
        {
            var user = await FindUserByEmailOrUsername(request);
            if(user != null && user.State == UserState.Active)
            {
                return await _requestPasswordResetUseCase.Execute(user.Id ?? 0);
            }
            return Result<bool>.Success(true);
        }
    }
}