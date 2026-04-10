namespace Mercadito.src.shared.domain.validation
{
    public sealed class StringRuleSet
    {
        private readonly Dictionary<string, List<Func<string, string>>> _rules = [];

        public void Add(string field, Func<string, string> rule)
        {
            if (!_rules.TryGetValue(field, out var rules))
            {
                rules = [];
                _rules[field] = rules;
            }

            rules.Add(rule);
        }

        public void Validate(string field, string value, ValidationErrorBag errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (!_rules.TryGetValue(field, out var rules))
            {
                return;
            }

            foreach (var rule in rules)
            {
                var message = rule(value);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    errors.Add(field, message);
                }
            }
        }
    }
}
