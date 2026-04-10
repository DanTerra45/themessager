using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.users.application.validation
{
    internal static class UserPasswordRules
    {
        internal static void AddPasswordErrors(ValidationErrorBag errors, string field, string password)
        {
            if (password.Length < 8 || password.Length > 128)
            {
                errors.Add(field, "La contraseña debe tener entre 8 y 128 caracteres.");
            }

            if (!ContainsUpper(password))
            {
                errors.Add(field, "La contraseña debe incluir al menos una letra mayúscula.");
            }

            if (!ContainsLower(password))
            {
                errors.Add(field, "La contraseña debe incluir al menos una letra minúscula.");
            }

            if (!ContainsDigit(password))
            {
                errors.Add(field, "La contraseña debe incluir al menos un número.");
            }
        }

        private static bool ContainsUpper(string value)
        {
            foreach (var character in value)
            {
                if (char.IsUpper(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsLower(string value)
        {
            foreach (var character in value)
            {
                if (char.IsLower(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDigit(string value)
        {
            foreach (var character in value)
            {
                if (char.IsDigit(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
