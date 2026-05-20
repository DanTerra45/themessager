using Domain.Entities;
using Domain.Dto.Response;

namespace Users.Domain.Common;

public static class UserMappers
{
    public static UserResponse ToResponse(this User user) =>
        new(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role,
            NeedPasswordChange: user.NeedPasswordChange,
            State: user.State
        );
}