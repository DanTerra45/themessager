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
    public sealed class UpdateSaleFacade(
        ISalesRepository salesRepository,
        IUpdateSaleValidator validator,
        IAuditTrailService auditTrailService) : IUpdateSaleFacade
    {
        public async Task<Result<SaleReceiptDto>> UpdateAsync(UpdateSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<SaleReceiptDto>(actorValidation.ErrorMessage);
            }

            var validationResult = validator.Validate(request);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure<SaleReceiptDto>(validationResult.Errors);
                }

                return Result.Failure<SaleReceiptDto>(validationResult.ErrorMessage);
            }

            try
            {
                var normalizedRequest = validationResult.Value;
                var previousSale = await salesRepository.GetSaleDetailAsync(normalizedRequest.SaleId, cancellationToken);
                if (previousSale == null)
                {
                    return Result.Failure<SaleReceiptDto>("La venta solicitada no existe.");
                }

                var wasUpdated = await salesRepository.UpdateAsync(normalizedRequest, cancellationToken);
                if (!wasUpdated)
                {
                    return Result.Failure<SaleReceiptDto>("La venta no pudo actualizarse porque no existe o ya fue anulada.");
                }

                var updatedSale = await salesRepository.GetSaleDetailAsync(normalizedRequest.SaleId, cancellationToken);
                var receipt = await salesRepository.GetSaleReceiptAsync(normalizedRequest.SaleId, cancellationToken);
                if (updatedSale == null || receipt == null)
                {
                    return Result.Failure<SaleReceiptDto>("La venta se actualizó, pero no se pudo recargar su información.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "ventas",
                    normalizedRequest.SaleId,
                    previousSale,
                    updatedSale,
                    cancellationToken);

                return Result.Success(receipt);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<SaleReceiptDto>(validationException.Errors);
                }

                return Result.Failure<SaleReceiptDto>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<SaleReceiptDto>(validationException.Message);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SaleReceiptDto>(exception.Message);
            }
        }
    }
}
