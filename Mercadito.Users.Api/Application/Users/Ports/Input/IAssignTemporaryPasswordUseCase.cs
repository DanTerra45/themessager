using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;
using Mercadito.Users.Api.Application.Users.Models;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IAssignTemporaryPasswordUseCase
    {
        Task<Result<bool>> ExecuteAsync(AssignTemporaryPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
