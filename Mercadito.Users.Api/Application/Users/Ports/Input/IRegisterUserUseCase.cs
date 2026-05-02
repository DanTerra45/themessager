using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IRegisterUserUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateUserDto user, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
