using ServicioCatalogo.Application.Audit.Ports.Input;
using ServicioCatalogo.Application.Audit.Ports.Output;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;
using ServicioCatalogo.Domain.Shared.Exceptions;

namespace ServicioCatalogo.Application.Audit.UseCases
{
    public sealed class RegisterAuditEntryUseCase(IAuditRepository auditRepository) : IRegisterAuditEntryUseCase
    {
        public async Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
            {
                return Result.Failure("La entrada de auditoría es obligatoria.");
            }

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

            try
            {
                await auditRepository.RegisterAsync(entry, cancellationToken);
                return Result.Success();
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure("Se produjo un error inesperado al procesar la solicitud.");
            }
        }
    }
}
