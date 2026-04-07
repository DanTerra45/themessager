using Mercadito.src.audit.application.ports.output;
using Mercadito.src.audit.application.ports.input;
using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.audit.application.use_cases
{
    public sealed class RegisterAuditEntryUseCase : IRegisterAuditEntryUseCase
    {
        private readonly IAuditRepository _auditRepository;

        public RegisterAuditEntryUseCase(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
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

            await _auditRepository.RegisterAsync(entry, cancellationToken);
            return Result.Success();
        }
    }
}
