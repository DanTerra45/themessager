using System.ComponentModel.DataAnnotations;
using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.application.sales.validation
{
    public sealed class RegisterSaleValidator : IRegisterSaleValidator
    {
        public Result<RegisterSaleDto> Validate(RegisterSaleDto input)
        {
            if (input == null)
            {
                return Result.Failure<RegisterSaleDto>("La venta es obligatoria.");
            }

            var normalized = SaleValidationNormalization.Normalize(input);
            var errors = SaleValidationRules.ValidateRegister(normalized);
            if (errors.HasErrors)
            {
                return Result.Failure<RegisterSaleDto>(errors.ToDictionary());
            }

            return Result.Success(normalized);
        }
    }

    public sealed class UpdateSaleValidator : IUpdateSaleValidator
    {
        public Result<UpdateSaleDto> Validate(UpdateSaleDto input)
        {
            if (input == null)
            {
                return Result.Failure<UpdateSaleDto>("La venta es obligatoria.");
            }

            var normalized = SaleValidationNormalization.Normalize(input);
            var errors = SaleValidationRules.ValidateUpdate(normalized);
            if (errors.HasErrors)
            {
                return Result.Failure<UpdateSaleDto>(errors.ToDictionary());
            }

            return Result.Success(normalized);
        }
    }

    public sealed class CancelSaleValidator : ICancelSaleValidator
    {
        public Result<CancelSaleDto> Validate(CancelSaleDto input)
        {
            if (input == null)
            {
                return Result.Failure<CancelSaleDto>("La anulación es obligatoria.");
            }

            var normalized = SaleValidationNormalization.Normalize(input);
            var errors = SaleValidationRules.ValidateCancel(normalized);
            if (errors.HasErrors)
            {
                return Result.Failure<CancelSaleDto>(errors.ToDictionary());
            }

            return Result.Success(normalized);
        }
    }

    internal static class SaleValidationNormalization
    {
        internal static RegisterSaleDto Normalize(RegisterSaleDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new RegisterSaleDto
            {
                CustomerId = input.CustomerId,
                NewCustomer = NormalizeCustomer(input.NewCustomer),
                PaymentMethod = ValidationText.NormalizeCollapsed(input.PaymentMethod),
                Channel = ValidationText.NormalizeCollapsed(input.Channel),
                Lines = MergeLines(input.Lines)
            };
        }

        internal static UpdateSaleDto Normalize(UpdateSaleDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new UpdateSaleDto
            {
                SaleId = input.SaleId,
                CustomerId = input.CustomerId,
                NewCustomer = NormalizeCustomer(input.NewCustomer),
                PaymentMethod = ValidationText.NormalizeCollapsed(input.PaymentMethod),
                Channel = ValidationText.NormalizeCollapsed(input.Channel),
                Lines = MergeLines(input.Lines)
            };
        }

        internal static CancelSaleDto Normalize(CancelSaleDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new CancelSaleDto
            {
                SaleId = input.SaleId,
                Reason = ValidationText.NormalizeCollapsed(input.Reason)
            };
        }

        private static CreateCustomerDto NormalizeCustomer(CreateCustomerDto? customer)
        {
            customer ??= new CreateCustomerDto();

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

        private static List<RegisterSaleLineDto> MergeLines(IReadOnlyList<RegisterSaleLineDto>? lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return [];
            }

            var quantitiesByProductId = new Dictionary<long, int>();
            var orderedProductIds = new List<long>();
            foreach (var line in lines)
            {
                if (line.ProductId <= 0 || line.Quantity <= 0)
                {
                    continue;
                }

                if (!quantitiesByProductId.TryGetValue(line.ProductId, out var currentQuantity))
                {
                    quantitiesByProductId[line.ProductId] = line.Quantity;
                    orderedProductIds.Add(line.ProductId);
                    continue;
                }

                quantitiesByProductId[line.ProductId] = currentQuantity + line.Quantity;
            }

            var mergedLines = new List<RegisterSaleLineDto>(orderedProductIds.Count);
            foreach (var productId in orderedProductIds)
            {
                mergedLines.Add(new RegisterSaleLineDto
                {
                    ProductId = productId,
                    Quantity = quantitiesByProductId[productId]
                });
            }

            return mergedLines;
        }
    }

    internal static class SaleValidationRules
    {
        internal static ValidationErrorBag ValidateRegister(RegisterSaleDto request)
        {
            var errors = new ValidationErrorBag();
            ValidateObject(request, string.Empty, errors);

            if (request.CustomerId <= 0)
            {
                ValidateObject(request.NewCustomer, nameof(RegisterSaleDto.NewCustomer), errors);
            }

            ValidateLines(request.Lines, nameof(RegisterSaleDto.Lines), errors);
            return errors;
        }

        internal static ValidationErrorBag ValidateUpdate(UpdateSaleDto request)
        {
            var errors = new ValidationErrorBag();
            ValidateObject(request, string.Empty, errors);

            if (request.CustomerId <= 0)
            {
                ValidateObject(request.NewCustomer, nameof(UpdateSaleDto.NewCustomer), errors);
            }

            ValidateLines(request.Lines, nameof(UpdateSaleDto.Lines), errors);
            return errors;
        }

        internal static ValidationErrorBag ValidateCancel(CancelSaleDto request)
        {
            var errors = new ValidationErrorBag();
            ValidateObject(request, string.Empty, errors);
            return errors;
        }

        private static void ValidateLines(IReadOnlyList<RegisterSaleLineDto> lines, string fieldName, ValidationErrorBag errors)
        {
            if (lines.Count == 0)
            {
                errors.Add(fieldName, "Debes agregar al menos un producto a la venta.");
                return;
            }

            for (var index = 0; index < lines.Count; index++)
            {
                ValidateObject(lines[index], $"{fieldName}[{index}]", errors);
            }
        }

        private static void ValidateObject(object? instance, string prefix, ValidationErrorBag errors)
        {
            if (instance == null)
            {
                errors.Add(prefix, "El valor es obligatorio.");
                return;
            }

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
