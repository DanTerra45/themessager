using Mercadito.Users.Api.Application.Audit.Ports.Output;
using Mercadito.Users.Api.Application.Audit.Ports.Input;
using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Audit.UseCases
{
    public sealed class RegisterAuditEntryUseCase(IAuditRepository auditRepository) : IRegisterAuditEntryUseCase
    {
        public async Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.UserId <= 0)
            {
                return Result.Failure("El usuario de auditoría es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(entry.Username))
            {
                return Result.Failure("El nombre de usuario de auditoría es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(entry.TableName))
            {
                return Result.Failure("La tabla auditada es obligatoria.");
            }

            if (entry.RecordId <= 0)
            {
                return Result.Failure("El identificador del registro auditado es obligatorio.");
            }

            await auditRepository.RegisterAsync(entry, cancellationToken);
            return Result.Success();
        }
    }
}
