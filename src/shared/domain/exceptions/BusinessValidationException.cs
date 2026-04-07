using System;

namespace Shared.Domain
{
    public sealed class BusinessValidationException : Exception
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public IReadOnlyDictionary<string, List<string>> Errors => _errors;

        public BusinessValidationException(string message)
            : base(message)
        {
        }

        public BusinessValidationException(string field, string message)
            : this(new Dictionary<string, List<string>>
            {
                [field] = [message]
            })
        {
        }

        public BusinessValidationException(IReadOnlyDictionary<string, List<string>> errors)
            : base(BuildMessage(errors))
        {
            foreach (var error in errors)
            {
                _errors[error.Key] = new List<string>(error.Value);
            }
        }

        private static string BuildMessage(IReadOnlyDictionary<string, List<string>> errors)
        {
            foreach (var error in errors)
            {
                if (error.Value.Count > 0 && !string.IsNullOrWhiteSpace(error.Value[0]))
                {
                    return error.Value[0];
                }
            }

            return "La operación no pudo completarse por errores de validación.";
        }
    }
}
