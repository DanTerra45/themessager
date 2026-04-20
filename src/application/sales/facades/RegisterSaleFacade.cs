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
    public sealed class RegisterSaleFacade(
        ISalesRepository salesRepository,
        IRegisterSaleValidator validator,
        IAuditTrailService auditTrailService) : IRegisterSaleFacade
    {
        public async Task<Result<SaleReceiptDto>> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
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
                var saleId = await salesRepository.RegisterAsync(normalizedRequest, actor, cancellationToken);
                var receipt = await salesRepository.GetSaleReceiptAsync(saleId, cancellationToken);
                if (receipt == null)
                {
                    return Result.Failure<SaleReceiptDto>("La venta se registró, pero no se pudo cargar el comprobante.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Create,
                    "ventas",
                    saleId,
                    null,
                    new
                    {
                        normalizedRequest.CustomerId,
                        normalizedRequest.PaymentMethod,
                        normalizedRequest.Channel,
                        LineCount = normalizedRequest.Lines.Count,
                        receipt.Total
                    },
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
