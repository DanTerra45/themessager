using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;
using Mercadito.Users.Api.Application.Users.Models;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IForcePasswordChangeUseCase
    {
        Task<Result<bool>> ExecuteAsync(long userId, ForcePasswordChangeDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
