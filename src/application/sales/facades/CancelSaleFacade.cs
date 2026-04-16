using System.ComponentModel.DataAnnotations;
using Mercadito.src.application.audit.services;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.application.sales.ports.output;
using Mercadito.src.application.sales.validation;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.src.application.sales.facades
{
    public sealed class CancelSaleFacade(
        ISalesRepository salesRepository,
        ICancelSaleValidator validator,
        IAuditTrailService auditTrailService) : ICancelSaleFacade
    {
        public async Task<Result<bool>> CancelAsync(CancelSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<bool>(actorValidation.ErrorMessage);
            }

            var validationResult = validator.Validate(request);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure<bool>(validationResult.Errors);
                }

                return Result.Failure<bool>(validationResult.ErrorMessage);
            }

            try
            {
                var normalizedRequest = validationResult.Value;
                var previousSale = await salesRepository.GetSaleDetailAsync(normalizedRequest.SaleId, cancellationToken);
                if (previousSale == null)
                {
                    return Result.Failure<bool>("La venta solicitada no existe.");
                }

                var wasCancelled = await salesRepository.CancelAsync(normalizedRequest.SaleId, normalizedRequest.Reason, actor, cancellationToken);
                if (!wasCancelled)
                {
                    return Result.Failure<bool>("La venta no pudo anularse o ya se encontraba anulada.");
                }

                var updatedSale = await salesRepository.GetSaleDetailAsync(normalizedRequest.SaleId, cancellationToken);
                object updatedAuditSnapshot;
                if (updatedSale == null)
                {
                    updatedAuditSnapshot = new
                    {
                        previousSale.Id,
                        Estado = "Anulada",
                        MotivoAnulacion = normalizedRequest.Reason
                    };
                }
                else
                {
                    updatedAuditSnapshot = updatedSale;
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "ventas",
                    normalizedRequest.SaleId,
                    previousSale,
                    updatedAuditSnapshot,
                    cancellationToken);

                return Result.Success(true);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<bool>(validationException.Errors);
                }

                return Result.Failure<bool>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<bool>(validationException.Message);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<bool>(exception.Message);
            }
        }
    }
}
