namespace Domain.Database
{
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like,
        ILike,
        Between,
        IsNull,
        IsNotNull
    }
    public record FilterCondition<TFields>(
        TFields Field,
        FilterOperator Operator,
        object? Value) where TFields : Enum;
    public interface IQueryOptions<TFields>
        where TFields : Enum
    {
        int? Limit { get; set; }
        int? Offset { get; set; }
        TFields OrderBy { get; set; }           // default(TFields) = "sin orden"
        bool OrderDescending { get; set; }
        IEnumerable<TFields> SelectedFields { get; set; }
        List<FilterCondition<TFields>> Filters { get; set; }
        IQueryOptions<TFields> AddFilter(TFields field, FilterOperator op, object? value);
    }
}
