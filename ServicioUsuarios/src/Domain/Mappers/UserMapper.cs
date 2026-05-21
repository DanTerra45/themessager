using Domain.Dto.Response;
using Domain.Entities;

namespace Domain.Mappers
{
    public static class UserMapper
    {
        public static UserResponse ToResponse(this User user)
        {
            return new UserResponse(
                Id:user.Id,
                Username:user.Username,
                Email:user.Email,
                Role:user.Role.ToString(),
                NeedPasswordChange:user.NeedPasswordChange,
                State:user.State.ToString()
            );
            
        }
    }
}