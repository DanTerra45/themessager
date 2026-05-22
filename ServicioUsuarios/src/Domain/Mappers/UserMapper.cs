using Application.utils;
using Domain.Dto.Jwt;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Entities.Enums;

namespace Domain.Mappers
{
    public static class UserMapper
    {
        public static UserResponse ToResponse(this User user)
        {
            return new UserResponse(
                Id:user?.Id ?? 0,
                Username:user.Username,
                Email:user.Email,
                Role:user.Role.ToString(),
                NeedPasswordChange:user.NeedPasswordChange,
                State:user.State.ToString()
            );
        }
        public static User ToEntity(this RegisterUserDto dto)
        {
            if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role))
            {
                throw new ArgumentException($"Invalid role value: {dto.Role}");
            }

            return new User
            {
                Id = null,
                Username = dto.Username,
                Email = dto.Email,
                Password = PasswordUtils.HashPassword(dto.Password),
                Role = role,
                NeedPasswordChange = true,
                State = UserState.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                LastLogin = null,
                CreatorId = dto.CreatorId
            };
        }
        public static RegisterUserDto ToRegisterDto(this CreateUserDto dto,int creatorId)
        {
            return new RegisterUserDto(
                Username: dto.Username,
                Email: dto.Email,
                Password: dto.Password,
                Role: dto.Role,
                CreatorId: creatorId
            );
        }
        public static JwtUserPayload ToJwtPayload(this User user)
        {
            return new JwtUserPayload(
                UserId: user.Id ?? 0,
                Username: user.Username,
                Email: user.Email,
                Role: user.Role.ToString()
            );
        }
    }
}