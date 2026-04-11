using Mercadito.src.categories.application.models;
using Mercadito.src.categories.domain.entities;
using Mercadito.src.categories.domain.factories;
using Mercadito.src.shared.domain.validation;
using Mercadito.src.shared.domain;

namespace Mercadito.src.categories.application.validation
{
    public interface ICreateCategoryValidator : IValidator<CreateCategoryDto, Category>
    {
    }

    public interface IUpdateCategoryValidator : IValidator<UpdateCategoryDto, Category>
    {
    }

    public abstract class CategoryValidator
    {
        private const string CategoryCodePattern = "^C[0-9]{5}$";

        private readonly StringRuleSet _stringRules = new();
        private readonly ValidationErrorBag _errors = new();

        protected CategoryValidator()
        {
            ConfigureCodeRules();
            ConfigureNameRules();
            ConfigureDescriptionRules();
        }

        private void ConfigureCodeRules()
        {
            AddStringRule("Code", StringValidationRules.Required("El código es obligatorio"));
            AddStringRule("Code", StringValidationRules.ExactLength(6, "El código debe tener exactamente 6 caracteres"));
            AddStringRule("Code", StringValidationRules.RegexMatch(CategoryCodePattern, "El código debe tener formato C00001"));
        }

        private void ConfigureNameRules()
        {
            AddStringRule("Name", StringValidationRules.Required("El nombre es obligatorio"));
            AddStringRule("Name", StringValidationRules.MaxLength(150, "El nombre no puede exceder 150 caracteres"));
            AddStringRule("Name", StringValidationRules.ControlCharacters("El nombre contiene caracteres no permitidos"));
        }

        private void ConfigureDescriptionRules()
        {
            AddStringRule("Description", StringValidationRules.Required("La descripción es obligatoria"));
            AddStringRule("Description", StringValidationRules.MaxLength(150, "La descripción no puede exceder 150 caracteres"));
            AddStringRule("Description", StringValidationRules.ControlCharacters("La descripción contiene caracteres no permitidos"));
        }

        protected void ValidateCreateFields(CreateCategoryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            ValidateField("Code", dto.Code);
            ValidateField("Name", dto.Name);
            ValidateField("Description", dto.Description);
        }

        protected void ValidateUpdateFields(UpdateCategoryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Id <= 0)
            {
                AddError("Id", "La categoría es inválida.");
            }

            ValidateField("Code", dto.Code);
            ValidateField("Name", dto.Name);
            ValidateField("Description", dto.Description);
        }

        protected void ValidateField(string field, string value)
        {
            _stringRules.Validate(field, value, _errors);
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

    public sealed class CreateCategoryValidator(ICategoryFactory categoryFactory) : CategoryValidator, ICreateCategoryValidator
    {
        public Result<Category> Validate(CreateCategoryDto input)
        {
            if (input == null)
            {
                return Result.Failure<Category>("La categoría es obligatoria.");
            }

            var normalizedInput = CategoryValidationNormalization.NormalizeCreateInput(input);
            ClearErrors();
            ValidateCreateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Category>(GetErrors());
            }

            return Result.Success(categoryFactory.CreateForInsert(CategoryValidationNormalization.ToCreateValues(normalizedInput)));
        }
    }

    public sealed class UpdateCategoryValidator(ICategoryFactory categoryFactory) : CategoryValidator, IUpdateCategoryValidator
    {
        public Result<Category> Validate(UpdateCategoryDto input)
        {
            if (input == null)
            {
                return Result.Failure<Category>("La categoría es obligatoria.");
            }

            var normalizedInput = CategoryValidationNormalization.NormalizeUpdateInput(input);
            ClearErrors();
            ValidateUpdateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Category>(GetErrors());
            }

            return Result.Success(categoryFactory.CreateForUpdate(CategoryValidationNormalization.ToUpdateValues(normalizedInput)));
        }
    }

    internal static class CategoryValidationNormalization
    {
        internal static CreateCategoryDto NormalizeCreateInput(CreateCategoryDto input)
        {
            return new CreateCategoryDto
            {
                Code = ValidationText.NormalizeUpperTrimmed(input.Code),
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description)
            };
        }

        internal static CreateCategoryValues ToCreateValues(CreateCategoryDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new CreateCategoryValues(
                input.Code,
                input.Name,
                input.Description);
        }

        internal static UpdateCategoryDto NormalizeUpdateInput(UpdateCategoryDto input)
        {
            return new UpdateCategoryDto
            {
                Id = input.Id,
                Code = ValidationText.NormalizeUpperTrimmed(input.Code),
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description)
            };
        }

        internal static UpdateCategoryValues ToUpdateValues(UpdateCategoryDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new UpdateCategoryValues(
                input.Id,
                input.Code,
                input.Name,
                input.Description);
        }
    }
}
