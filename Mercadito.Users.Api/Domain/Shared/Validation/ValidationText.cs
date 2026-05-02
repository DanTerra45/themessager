using System.Text;

namespace Mercadito.Users.Api.Domain.Shared.Validation
{
    public static class ValidationText
    {
        public static string NormalizeTrimmed(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }

        public static string NormalizeLowerTrimmed(string? value)
        {
            var normalizedValue = NormalizeTrimmed(value);
            return normalizedValue.ToLowerInvariant();
        }

        public static string NormalizeUpperTrimmed(string? value)
        {
            var normalizedValue = NormalizeTrimmed(value);
            return normalizedValue.ToUpperInvariant();
        }

        public static string NormalizeCollapsed(string? value)
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

        public static string NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return NormalizeCollapsed(value);
        }
    }
}
