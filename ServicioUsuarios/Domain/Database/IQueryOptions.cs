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
    public enum LogicalOperator
    {
        And,
        Or
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
        TFields OrderBy { get; set; }     
        bool OrderDescending { get; set; }
        IEnumerable<TFields> SelectedFields { get; set; }
        List<FilterCondition<TFields>> Filters { get; set; }
        IQueryOptions<TFields> AddFilter(TFields field, FilterOperator op, object? value);
        IQueryOptions<TFields> AddLogicalFilter(IEnumerable<FilterCondition<TFields>> conditions, LogicalOperator or);
    }
}
