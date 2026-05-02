using System.Text.RegularExpressions;

namespace Mercadito.Users.Api.Domain.Shared.Validation
{
    public static class StringValidationRules
    {
        public static Func<string, string> Required(string message)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return message;
                }

                return string.Empty;
            };
        }

        public static Func<string, string> LengthBetween(int minimum, int maximum, string message)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                if (value.Length >= minimum && value.Length <= maximum)
                {
                    return string.Empty;
                }

                return message;
            };
        }

        public static Func<string, string> ExactLength(int length, string message)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                if (value.Length == length)
                {
                    return string.Empty;
                }

                return message;
            };
        }

        public static Func<string, string> MaxLength(int maximum, string message)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                if (value.Length <= maximum)
                {
                    return string.Empty;
                }

                return message;
            };
        }

        public static Func<string, string> RegexMatch(string pattern, string message)
        {
            return RegexMatch(pattern, message, RegexOptions.CultureInvariant);
        }

        public static Func<string, string> RegexMatch(string pattern, string message, RegexOptions options)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                if (Regex.IsMatch(value, pattern, options))
                {
                    return string.Empty;
                }

                return message;
            };
        }

        public static Func<string, string> OneOf(IEnumerable<string> allowedValues, string message, StringComparer? comparer = null)
        {
            if (comparer == null)
            {
                comparer = StringComparer.Ordinal;
            }

            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                foreach (var allowedValue in allowedValues)
                {
                    if (comparer.Equals(value, allowedValue))
                    {
                        return string.Empty;
                    }
                }

                return message;
            };
        }

        public static Func<string, string> AbsoluteUri(string message)
        {
            return value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                if (Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    return string.Empty;
                }

                return message;
            };
        }

        public static Func<string, string> ControlCharacters(string message)
        {
            return value =>
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
            };
        }
    }
}
