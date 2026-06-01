using Application.Utils;
using Domain.Dto.Jwt;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Entities.Enums;

namespace Domain.Mappers
{
    public static class UserMapper
    {
        private static bool TryResolveRole(string? roleValue, out UserRole role)
        {
            role = default;
            if (string.IsNullOrWhiteSpace(roleValue))
            {
                return false;
            }

            var normalizedRole = roleValue.Trim();
            switch (normalizedRole.ToLowerInvariant())
            {
                case "admin":
                    role = UserRole.Admin;
                    return true;
                case "auditor":
                    role = UserRole.Auditor;
                    return true;
                case "operator":
                case "operador":
                    role = UserRole.Operator;
                    return true;
            }

            return Enum.TryParse<UserRole>(normalizedRole, ignoreCase: true, out role);
        }

        public static UserResponse ToResponse(this User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            return new UserResponse(
                Id:user.Id ?? 0,
                Username:user.Username,
                Email:user.Email,
                Role:user.Role.ToString(),
                NeedPasswordChange:user.NeedPasswordChange,
                State:user.State.ToString(),
                LastLogin:user.LastLogin,
                CreatedAt:user.CreatedAt
            );
        }
        public static User ToEntity(this RegisterUserDto dto)
        {
            if (!TryResolveRole(dto.Role, out var role))
            {
                throw new ArgumentException($"Invalid role value: {dto.Role}");
            }

            return new User
            {
                Id = null,
                Username = dto.Username,
                Email = dto.Email,
                Password = dto.Password,
                Role = role,
                NeedPasswordChange = true,
                State = UserState.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                LastLogin = null,
                CreatorId = dto.CreatorId
            };
        }
        public static RegisterUserDto ToRegisterDto(this CreateUserDto dto,int creatorId,string? password = null)
        {
            return new RegisterUserDto(
                Username: dto.Username,
                Email: dto.Email,
                Password: password ?? "TempPass123",
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
        public static UserStory ToUserStory(this RegisterUserStoryDto dto)
        {
            if (!int.TryParse(dto.OperatorId, out var operatorId))
            {
                throw new ArgumentException($"Invalid operator id value: {dto.OperatorId}");
            }

            return new UserStory
            {
                Id = null,
                UserId = dto.UserId,
                OperatorId = operatorId,
                PreviousState = dto.PreviousState ?? UserState.Active,
                NewState = dto.NewState ?? UserState.Inactive,
                DisableReason = dto.DisableReason,
                CreatedAt = dto.CreatedAt ?? DateTime.UtcNow
            };
        }
    }
}
