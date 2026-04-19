using System.ComponentModel.DataAnnotations;
using Mercadito.src.application.audit.services;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.application.sales.ports.output;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.application.sales.facades
{
    public sealed class SalesTransactionFacade(
        ISalesRepository salesRepository,
        IAuditTrailService auditTrailService) : ISalesTransactionFacade
    {
        public async Task<Result<SalesRegistrationContext>> LoadRegistrationContextAsync(string customerSearchTerm = "", string productSearchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedCustomerSearchTerm = ValidationText.NormalizeCollapsed(customerSearchTerm);
                var normalizedProductSearchTerm = ValidationText.NormalizeCollapsed(productSearchTerm);
                var nextSaleCode = await salesRepository.GetNextSaleCodeAsync(cancellationToken);
                var customers = await salesRepository.SearchCustomersAsync(normalizedCustomerSearchTerm, cancellationToken);
                var products = await salesRepository.SearchProductsAsync(normalizedProductSearchTerm, cancellationToken);

                return Result.Success(new SalesRegistrationContext
                {
                    NextSaleCode = nextSaleCode,
                    Customers = customers,
                    Products = products
                });
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<SalesRegistrationContext>(validationException.Errors);
                }

                return Result.Failure<SalesRegistrationContext>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<SalesRegistrationContext>(validationException.Message);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SalesRegistrationContext>(exception.Message);
            }
        }

        public async Task<Result<SaleReceiptDto>> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<SaleReceiptDto>(actorValidation.ErrorMessage);
            }

            var normalizedRequest = NormalizeRequest(request);
            var errors = ValidateRegisterRequest(normalizedRequest);
            if (errors.HasErrors)
            {
                return Result.Failure<SaleReceiptDto>(errors.ToDictionary());
            }

            try
            {
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

        public async Task<Result<IReadOnlyList<SaleSummaryItem>>> GetRecentSalesAsync(int take = 20, string sortBy = "createdat", string sortDirection = "desc", CancellationToken cancellationToken = default)
        {
            var normalizedTake = 20;
            if (take > 0)
            {
                normalizedTake = Math.Min(take, 100);
            }

            try
            {
                var sales = await salesRepository.GetRecentSalesAsync(normalizedTake, sortBy, sortDirection, cancellationToken);
                return Result.Success<IReadOnlyList<SaleSummaryItem>>(sales);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<IReadOnlyList<SaleSummaryItem>>(exception.Message);
            }
        }

        public async Task<Result<SalesOverviewMetrics>> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var metrics = await salesRepository.GetOverviewMetricsAsync(cancellationToken);
                return Result.Success(metrics);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SalesOverviewMetrics>(exception.Message);
            }
        }

        public async Task<Result<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
        {
            if (saleId <= 0)
            {
                return Result.Failure<SaleDetailDto>("El identificador de la venta es inválido.");
            }

            try
            {
                var saleDetail = await salesRepository.GetSaleDetailAsync(saleId, cancellationToken);
                if (saleDetail == null)
                {
                    return Result.Failure<SaleDetailDto>("La venta solicitada no existe.");
                }

                return Result.Success(saleDetail);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SaleDetailDto>(exception.Message);
            }
        }

        public async Task<Result<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
        {
            if (saleId <= 0)
            {
                return Result.Failure<SaleReceiptDto>("El identificador de la venta es inválido.");
            }

            try
            {
                var receipt = await salesRepository.GetSaleReceiptAsync(saleId, cancellationToken);
                if (receipt == null)
                {
                    return Result.Failure<SaleReceiptDto>("No se encontró el comprobante solicitado.");
                }

                return Result.Success(receipt);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SaleReceiptDto>(exception.Message);
            }
        }

        public async Task<Result<bool>> CancelAsync(CancelSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<bool>(actorValidation.ErrorMessage);
            }

            var normalizedRequest = NormalizeCancelRequest(request);
            var errors = ValidateCancelRequest(normalizedRequest);
            if (errors.HasErrors)
            {
                return Result.Failure<bool>(errors.ToDictionary());
            }

            try
            {
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

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Delete,
                    "ventas",
                    normalizedRequest.SaleId,
                    previousSale,
                    new
                    {
                        Estado = "Anulada",
                        MotivoAnulacion = normalizedRequest.Reason
                    },
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

        private static RegisterSaleDto NormalizeRequest(RegisterSaleDto request)
        {
            return new RegisterSaleDto
            {
                CustomerId = request.CustomerId,
                NewCustomer = NormalizeCustomer(request.NewCustomer),
                PaymentMethod = ValidationText.NormalizeCollapsed(request.PaymentMethod),
                Channel = ValidationText.NormalizeCollapsed(request.Channel),
                Lines = MergeLines(request.Lines)
            };
        }

        private static CancelSaleDto NormalizeCancelRequest(CancelSaleDto request)
        {
            return new CancelSaleDto
            {
                SaleId = request.SaleId,
                Reason = ValidationText.NormalizeCollapsed(request.Reason)
            };
        }

        private static CreateCustomerDto NormalizeCustomer(CreateCustomerDto customer)
        {
            return new CreateCustomerDto
            {
                DocumentNumber = ValidationText.NormalizeUpperTrimmed(customer.DocumentNumber),
                BusinessName = ValidationText.NormalizeCollapsed(customer.BusinessName),
                Phone = NormalizeOptionalCustomerValue(customer.Phone, ValidationText.NormalizeTrimmed),
                Email = NormalizeOptionalCustomerValue(customer.Email, ValidationText.NormalizeLowerTrimmed),
                Address = NormalizeOptionalCustomerValue(customer.Address, ValidationText.NormalizeCollapsed)
            };
        }

        private static string? NormalizeOptionalCustomerValue(string? value, Func<string?, string> normalizer)
        {
            var normalizedValue = normalizer(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return normalizedValue;
        }

        private static List<RegisterSaleLineDto> MergeLines(IReadOnlyList<RegisterSaleLineDto> lines)
        {
            var quantitiesByProductId = new Dictionary<long, int>();
            foreach (var line in lines)
            {
                if (line.ProductId <= 0 || line.Quantity <= 0)
                {
                    continue;
                }

                if (!quantitiesByProductId.TryGetValue(line.ProductId, out var currentQuantity))
                {
                    quantitiesByProductId[line.ProductId] = line.Quantity;
                    continue;
                }

                quantitiesByProductId[line.ProductId] = currentQuantity + line.Quantity;
            }

            var mergedLines = new List<RegisterSaleLineDto>(quantitiesByProductId.Count);
            foreach (var quantityByProductId in quantitiesByProductId)
            {
                mergedLines.Add(new RegisterSaleLineDto
                {
                    ProductId = quantityByProductId.Key,
                    Quantity = quantityByProductId.Value
                });
            }

            return mergedLines;
        }

        private static ValidationErrorBag ValidateRegisterRequest(RegisterSaleDto request)
        {
            var errors = new ValidationErrorBag();
            ValidateObject(request, string.Empty, errors);

            if (request.CustomerId <= 0)
            {
                ValidateObject(request.NewCustomer, nameof(RegisterSaleDto.NewCustomer), errors);
            }

            if (request.Lines.Count == 0)
            {
                errors.Add(nameof(RegisterSaleDto.Lines), "Debes agregar al menos un producto a la venta.");
            }

            for (var index = 0; index < request.Lines.Count; index++)
            {
                ValidateObject(request.Lines[index], $"{nameof(RegisterSaleDto.Lines)}[{index}]", errors);
            }

            return errors;
        }

        private static ValidationErrorBag ValidateCancelRequest(CancelSaleDto request)
        {
            var errors = new ValidationErrorBag();
            ValidateObject(request, string.Empty, errors);
            return errors;
        }

        private static void ValidateObject(object instance, string prefix, ValidationErrorBag errors)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(instance);
            Validator.TryValidateObject(instance, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                var message = validationResult.ErrorMessage;
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Valor inválido.";
                }

                var hasMemberNames = false;
                foreach (var memberName in validationResult.MemberNames)
                {
                    hasMemberNames = true;
                    var key = memberName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        key = string.Concat(prefix, ".", memberName);
                    }

                    errors.Add(key, message);
                }

                if (!hasMemberNames)
                {
                    errors.Add(prefix, message);
                }
            }
        }
    }
}
