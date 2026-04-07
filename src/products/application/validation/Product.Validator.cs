using Mercadito.src.products.application.models;
using Mercadito.src.products.domain.entities;
using Mercadito.src.products.domain.factories;
using Mercadito.src.shared.domain.validator;
using Shared.Domain;
using System.Text.RegularExpressions;

namespace Mercadito.src.products.application.validation
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

        private readonly Dictionary<string, List<Func<string, string>>> _stringRules = new();
        protected readonly Dictionary<string, List<string>> errors = new();

        protected ProductValidator()
        {
            ConfigureNameRules();
            ConfigureDescriptionRules();
            ConfigureBatchRules();
        }

        private void ConfigureNameRules()
        {
            AddStringRule("Name", value => Required(value, "El nombre es obligatorio"));
            AddStringRule("Name", value => MaxLength(value, 150, "El nombre no puede exceder 150 caracteres"));
            AddStringRule("Name", value => ControlCharacters(value, "El nombre contiene caracteres no permitidos"));
        }

        private void ConfigureDescriptionRules()
        {
            AddStringRule("Description", value => Required(value, "La descripción es obligatoria"));
            AddStringRule("Description", value => MaxLength(value, 150, "La descripción no puede exceder 150 caracteres"));
            AddStringRule("Description", value => ControlCharacters(value, "La descripción contiene caracteres no permitidos"));
        }

        private void ConfigureBatchRules()
        {
            AddStringRule("Batch", value => Required(value, "Lote es obligatorio"));
            AddStringRule("Batch", value => MaxLength(value, 40, "Lote no puede exceder 40 caracteres"));
            AddStringRule("Batch", value => RegexMatch(value, BatchPattern, "El lote solo permite números"));
        }

        protected void ValidateCreateFields(CreateProductDto dto)
        {
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
            if (!_stringRules.TryGetValue(field, out var rules))
            {
                return;
            }

            foreach (var rule in rules)
            {
                var message = rule(value);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    AddError(field, message);
                }
            }
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

        protected void ValidateCategoryIds(IReadOnlyCollection<long> categoryIds)
        {
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
            if (!_stringRules.ContainsKey(field))
            {
                _stringRules[field] = [];
            }

            _stringRules[field].Add(rule);
        }

        protected void AddError(string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = [];
            }

            errors[field].Add(message);
        }

        protected void ClearErrors()
        {
            errors.Clear();
        }

        protected bool HasErrors()
        {
            return errors.Count > 0;
        }

        protected Dictionary<string, List<string>> GetErrors()
        {
            return errors;
        }

        private static string Required(string value, string message)
        {
            return string.IsNullOrWhiteSpace(value) ? message : string.Empty;
        }

        private static string MaxLength(string value, int maximum, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length <= maximum ? string.Empty : message;
        }

        private static string RegexMatch(string value, string pattern, string message)
        {
            return string.IsNullOrWhiteSpace(value) || Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant) ? string.Empty : message;
        }

        private static string ControlCharacters(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    return message;
                }
            }

            return string.Empty;
        }
    }

    public sealed class CreateProductValidator : ProductValidator, ICreateProductValidator
    {
        private readonly IProductFactory _productFactory;

        public CreateProductValidator(IProductFactory productFactory)
        {
            _productFactory = productFactory;
        }

        public Result<Product> Validate(CreateProductDto input)
        {
            if (input == null)
            {
                return Result<Product>.Failure("El producto es obligatorio.");
            }

            ClearErrors();
            ValidateCreateFields(input);

            return HasErrors()
                ? Result<Product>.Failure(GetErrors())
                : Result<Product>.Success(_productFactory.CreateForInsert(input));
        }
    }

    public sealed class UpdateProductValidator : ProductValidator, IUpdateProductValidator
    {
        private readonly IProductFactory _productFactory;

        public UpdateProductValidator(IProductFactory productFactory)
        {
            _productFactory = productFactory;
        }

        public Result<Product> Validate(UpdateProductDto input)
        {
            if (input == null)
            {
                return Result<Product>.Failure("El producto es obligatorio.");
            }

            ClearErrors();
            ValidateUpdateFields(input);

            return HasErrors()
                ? Result<Product>.Failure(GetErrors())
                : Result<Product>.Success(_productFactory.CreateForUpdate(input));
        }
    }
}
