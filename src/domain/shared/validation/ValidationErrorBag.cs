namespace Mercadito.src.domain.shared.validation
{
    public sealed class ValidationErrorBag
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public IReadOnlyDictionary<string, List<string>> Errors => _errors;

        public void Clear()
        {
            _errors.Clear();
        }

        public void Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var messages))
            {
                messages = [];
                _errors[field] = messages;
            }

            messages.Add(message);
        }

        public Dictionary<string, List<string>> ToDictionary()
        {
            var copy = new Dictionary<string, List<string>>(_errors.Count);
            foreach (var error in _errors)
            {
                copy[error.Key] = [.. error.Value];
            }

            return copy;
        }
    }
}
