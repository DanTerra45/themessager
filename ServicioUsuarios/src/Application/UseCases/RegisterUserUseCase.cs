using Application.Service;
using Application.Utils;
using Domain.Common;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Mappers;

namespace Application.UseCases
{
    public class RegisterUserUseCase
    {
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        public RegisterUserUseCase(UserService userService, EmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        public async Task<Result<int>> Execute(CreateUserDto createUserDto,int creatorId)
        {
            var (password, hashedPassword) = PasswordUtils.GenerateSecurePassword(20);
            var registerDto = createUserDto.ToRegisterDto(creatorId,hashedPassword);
            var createdUser = await _userService.CreateAsync(registerDto);
            if (!createdUser.IsSuccess) return createdUser;
            await _emailService.SendOnboardingAsync(
                createUserDto.Email,
                createUserDto.Username,
                createUserDto.Role,
                password,
                "http://localhost:5173/define-password");
            return Result<int>.Success(createdUser.Value);
        }
    }
}