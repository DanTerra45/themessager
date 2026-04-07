using System.Text;

namespace Mercadito.src.users.application.validation
{
    internal static class UserValidationHelpers
    {
        internal static string NormalizeCollapsed(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            var previousWasWhitespace = false;

            foreach (var character in value.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    if (previousWasWhitespace)
                    {
                        continue;
                    }

                    builder.Append(' ');
                    previousWasWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWasWhitespace = false;
            }

            return builder.ToString();
        }

        internal static void AddError(Dictionary<string, List<string>> errors, string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = [];
            }

            errors[field].Add(message);
        }
    }
}
