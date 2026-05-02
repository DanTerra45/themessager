using Mercadito.src.application.products.models;
using Mercadito.src.domain.products.entities;
using Mercadito.src.domain.products.factories;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.products.validation
{
    public interface ICreateProductValidator : IValidator<CreateProductDto, Product>
    {
    }

    public interface IUpdateProductValidator : IValidator<UpdateProductDto, Product>
    {
    }

    public abstract class ProductValidator
    {
        private const string BatchPattern = "^[0-9]{1,40}$";

        private readonly StringRuleSet _stringRules = new();
        private readonly ValidationErrorBag _errors = new();

        protected ProductValidator()
        {
            ConfigureNameRules();
            ConfigureDescriptionRules();
            ConfigureBatchRules();
        }

        private void ConfigureNameRules()
        {
            AddStringRule("Name", StringValidationRules.Required("El nombre es obligatorio"));
            AddStringRule("Name",StringValidationRules.RegexMatch(@"^[\p{L}\p{N}\s\.\-_,]+$", "El nombre solo permite letras, números, espacios y los caracteres . - _ ,"));
            AddStringRule("Name", StringValidationRules.MaxLength(150, "El nombre no puede exceder 150 caracteres"));
            AddStringRule("Name", StringValidationRules.ControlCharacters("El nombre contiene caracteres no permitidos"));
        }

        private void ConfigureDescriptionRules()
        {
            AddStringRule("Description", StringValidationRules.Required("La descripción es obligatoria"));
            AddStringRule("Description", StringValidationRules.MaxLength(150, "La descripción no puede exceder 150 caracteres"));
            AddStringRule("Description", StringValidationRules.ControlCharacters("La descripción contiene caracteres no permitidos"));
        }

        private void ConfigureBatchRules()
        {
            AddStringRule("Batch", StringValidationRules.Required("Lote es obligatorio"));
            AddStringRule("Batch", StringValidationRules.MaxLength(40, "Lote no puede exceder 40 caracteres"));
            AddStringRule("Batch", StringValidationRules.RegexMatch(BatchPattern, "El lote solo permite números"));
        }

        protected void ValidateCreateFields(CreateProductDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            ValidateField("Name", dto.Name);
            ValidateField("Description", dto.Description);
            ValidateField("Batch", dto.Batch);
            ValidateStock(dto.Stock);
            ValidateExpirationDate(dto.ExpirationDate);
            ValidatePrice(dto.Price);
            ValidateCategoryIds(dto.CategoryIds);
        }

        protected void ValidateUpdateFields(UpdateProductDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Id <= 0)
            {
                AddError("Id", "Id de producto inválido");
            }

            ValidateField("Name", dto.Name);
            ValidateField("Description", dto.Description);
            ValidateField("Batch", dto.Batch);
            ValidateStock(dto.Stock);
            ValidateExpirationDate(dto.ExpirationDate);
            ValidatePrice(dto.Price);
            ValidateCategoryIds(dto.CategoryIds);
        }

        protected void ValidateField(string field, string value)
        {
            _stringRules.Validate(field, value, _errors);
        }

        protected void ValidateStock(int? stock)
        {
            if (!stock.HasValue)
            {
                return;
            }

            if (stock.Value < 0)
            {
                AddError("Stock", "El stock debe ser un número positivo");
            }
        }

        protected void ValidateExpirationDate(DateOnly expirationDate)
        {
            if (expirationDate == default)
            {
                AddError("ExpirationDate", "La fecha de caducidad es obligatoria");
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (expirationDate < today)
            {
                AddError("ExpirationDate", "La fecha de caducidad no puede ser menor a hoy");
            }
        }

        protected void ValidatePrice(decimal? price)
        {
            if (!price.HasValue)
            {
                AddError("Price", "El Precio es obligatorio");
                return;
            }

            if (price.Value <= 0m)
            {
                AddError("Price", "El Precio debe ser un número decimal positivo");
            }
        }

        protected void ValidateCategoryIds(ICollection<long> categoryIds)
        {
            ArgumentNullException.ThrowIfNull(categoryIds);

            if (categoryIds.Count == 0)
            {
                AddError("CategoryIds", "Debe seleccionar al menos una categoría");
                return;
            }

            var distinctCategoryIds = new HashSet<long>();
            foreach (var categoryId in categoryIds)
            {
                if (categoryId <= 0)
                {
                    AddError("CategoryIds", "Las categorías seleccionadas son inválidas");
                    return;
                }

                if (!distinctCategoryIds.Add(categoryId))
                {
                    AddError("CategoryIds", "No puede repetir categorías para el mismo producto");
                    return;
                }
            }
        }

        protected void AddStringRule(string field, Func<string, string> rule)
        {
            _stringRules.Add(field, rule);
        }

        protected void AddError(string field, string message)
        {
            _errors.Add(field, message);
        }

        protected void ClearErrors()
        {
            _errors.Clear();
        }

        protected bool HasErrors()
        {
            return _errors.HasErrors;
        }

        protected Dictionary<string, List<string>> GetErrors()
        {
            return _errors.ToDictionary();
        }
    }

    public sealed class CreateProductValidator(IProductFactory productFactory) : ProductValidator, ICreateProductValidator
    {
        public Result<Product> Validate(CreateProductDto input)
        {
            if (input == null)
            {
                return Result.Failure<Product>("El producto es obligatorio.");
            }

            var normalizedInput = ProductValidationNormalization.NormalizeCreateInput(input);
            ClearErrors();
            ValidateCreateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Product>(GetErrors());
            }

            return Result.Success(productFactory.CreateForInsert(ProductValidationNormalization.ToCreateValues(normalizedInput)));
        }
    }

    public sealed class UpdateProductValidator(IProductFactory productFactory) : ProductValidator, IUpdateProductValidator
    {
        public Result<Product> Validate(UpdateProductDto input)
        {
            if (input == null)
            {
                return Result.Failure<Product>("El producto es obligatorio.");
            }

            var normalizedInput = ProductValidationNormalization.NormalizeUpdateInput(input);
            ClearErrors();
            ValidateUpdateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Product>(GetErrors());
            }

            return Result.Success(productFactory.CreateForUpdate(ProductValidationNormalization.ToUpdateValues(normalizedInput)));
        }
    }

    internal static class ProductValidationNormalization
    {
        internal static CreateProductDto NormalizeCreateInput(CreateProductDto input)
        {
            var normalizedInput = new CreateProductDto
            {
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description),
                Stock = input.Stock,
                Batch = ValidationText.NormalizeTrimmed(input.Batch),
                ExpirationDate = input.ExpirationDate,
                Price = input.Price
            };

            foreach (var categoryId in input.CategoryIds)
            {
                normalizedInput.CategoryIds.Add(categoryId);
            }

            return normalizedInput;
        }

        internal static CreateProductValues ToCreateValues(CreateProductDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new CreateProductValues(
                input.Name,
                input.Description,
                input.Stock,
                input.Batch,
                input.ExpirationDate,
                input.Price,
                [.. input.CategoryIds]);
        }

        internal static UpdateProductDto NormalizeUpdateInput(UpdateProductDto input)
        {
            var normalizedInput = new UpdateProductDto
            {
                Id = input.Id,
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description),
                Stock = input.Stock,
                Batch = ValidationText.NormalizeTrimmed(input.Batch),
                ExpirationDate = input.ExpirationDate,
                Price = input.Price
            };

            foreach (var categoryId in input.CategoryIds)
            {
                normalizedInput.CategoryIds.Add(categoryId);
            }

            return normalizedInput;
        }

        internal static UpdateProductValues ToUpdateValues(UpdateProductDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new UpdateProductValues(
                input.Id,
                input.Name,
                input.Description,
                input.Stock,
                input.Batch,
                input.ExpirationDate,
                input.Price,
                [.. input.CategoryIds]);
        }
    }
}
