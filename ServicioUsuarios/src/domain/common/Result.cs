namespace Users.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure => !IsSuccess;
        private readonly Dictionary<string,IEnumerable<string>> _errors = new();
        public IReadOnlyDictionary<string,IEnumerable<string>> Errors => _errors;
        protected Result(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
        public static Result Success() => new(true);
        public static Result Failure(string key, string errorMessage)
        {
            var result = new Result(false);
            result._errors.Add(key,new List<string>{errorMessage});
            return result;
        }
        public static Result Failure(Dictionary<string,IEnumerable<string>> errors)
        {
            var result = new Result(false);
            foreach (var error in errors)
            {
                result._errors.Add(error.Key,error.Value);
            }
            return result;
        }
        public void AddError(string key, string errorMessage)
        {
            if(!_errors.ContainsKey(key))
            {
                _errors[key] = new List<string>();
            }
            _errors[key].Add(errorMessage);
        }
    }
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T Value {get;}
        public bool IsFailure => !IsSuccess;
        private readonly Dictionary<string,IEnumerable<string>> _errors = new();
        public IReadOnlyDictionary<string,IEnumerable<string>> Errors => _errors;
        protected Result(bool isSuccess, T value)
        {
            IsSuccess = isSuccess;
            Value = value;
        }
        public static Result<T> Success(T value) => new(true,value);
        public static Result<T> Failure(string key, string errorMessage)
        {
            var result = new Result<T>(false,default);
            result._errors.Add(key,new List<string>{errorMessage});
            return result;
        }
        public static Result<T> Failure(Dictionary<string,IEnumerable<string>> errors)
        {
            var result = new Result<T>(false,default);
            foreach (var error in errors)
            {
                result._errors.Add(error.Key,error.Value);
            }
            return result;
        }
        public void AddError(string key, string errorMessage)
        {
            if(!_errors.ContainsKey(key))
            {
                _errors[key] = new List<string>();
            }
            _errors[key].Add(errorMessage);
        }
    }
}