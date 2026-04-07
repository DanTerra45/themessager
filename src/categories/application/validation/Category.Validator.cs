using Mercadito.src.categories.application.models;
using Mercadito.src.categories.domain.entities;
using Mercadito.src.categories.domain.factories;
using Mercadito.src.shared.domain.validator;
using Shared.Domain;
using System.Text.RegularExpressions;

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

        private readonly Dictionary<string, List<Func<string, string>>> _stringRules = new();
        protected readonly Dictionary<string, List<string>> errors = new();

        protected CategoryValidator()
        {
            ConfigureCodeRules();
            ConfigureNameRules();
            ConfigureDescriptionRules();
        }

        private void ConfigureCodeRules()
        {
            AddStringRule("Code", value => Required(value, "El código es obligatorio"));
            AddStringRule("Code", value => ExactLength(value, 6, "El código debe tener exactamente 6 caracteres"));
            AddStringRule("Code", value => RegexMatch(value, CategoryCodePattern, "El código debe tener formato C00001"));
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

        protected void ValidateCreateFields(CreateCategoryDto dto)
        {
            ValidateField("Code", dto.Code);
            ValidateField("Name", dto.Name);
            ValidateField("Description", dto.Description);
        }

        protected void ValidateUpdateFields(UpdateCategoryDto dto)
        {
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

        private static string MaxLength(string value, int maxLength, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length <= maxLength ? string.Empty : message;
        }

        private static string ExactLength(string value, int length, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length == length ? string.Empty : message;
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

    public sealed class CreateCategoryValidator : CategoryValidator, ICreateCategoryValidator
    {
        private readonly ICategoryFactory _categoryFactory;

        public CreateCategoryValidator(ICategoryFactory categoryFactory)
        {
            _categoryFactory = categoryFactory;
        }

        public Result<Category> Validate(CreateCategoryDto input)
        {
            if (input == null)
            {
                return Result<Category>.Failure("La categoría es obligatoria.");
            }

            ClearErrors();
            ValidateCreateFields(input);

            return HasErrors()
                ? Result<Category>.Failure(GetErrors())
                : Result<Category>.Success(_categoryFactory.CreateForInsert(input));
        }
    }

    public sealed class UpdateCategoryValidator : CategoryValidator, IUpdateCategoryValidator
    {
        private readonly ICategoryFactory _categoryFactory;

        public UpdateCategoryValidator(ICategoryFactory categoryFactory)
        {
            _categoryFactory = categoryFactory;
        }

        public Result<Category> Validate(UpdateCategoryDto input)
        {
            if (input == null)
            {
                return Result<Category>.Failure("La categoría es obligatoria.");
            }

            ClearErrors();
            ValidateUpdateFields(input);

            return HasErrors()
                ? Result<Category>.Failure(GetErrors())
                : Result<Category>.Success(_categoryFactory.CreateForUpdate(input));
        }
    }
}
