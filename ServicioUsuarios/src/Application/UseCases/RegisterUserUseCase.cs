using Application.Service;
using Domain.Common;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Mappers;

namespace Application.UseCases
{
    public class RegisterUserUseCase
    {
        private readonly UserService _userService;

        public RegisterUserUseCase(UserService userService)
        {
            _userService = userService;
        }

        public async Task<Result<int>> Execute(CreateUserDto createUserDto,int creatorId)
        {
            var registerDto = createUserDto.ToRegisterDto(creatorId);
            var createdUser = await _userService.CreateAsync(registerDto);
            return createdUser;
        }
    }
}