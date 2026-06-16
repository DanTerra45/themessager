using Domain.Database;

namespace Infrastructure.Options;

public class BaseQueryOptions<ITableField> : IQueryOptions<ITableField>
where ITableField : Enum
{
    
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public ITableField OrderBy { get; set; } = default!;
    public bool OrderDescending { get; set; } = false;
    public IEnumerable<ITableField> SelectedFields { get; set; } = new List<ITableField>();
    public List<FilterCondition<ITableField>> Filters { get; set; } = new();

    public IQueryOptions<ITableField> AddFilter(ITableField field, FilterOperator op, object? value)
    {
        Filters.Add(new(field, op, value));
        return this;
    }
    public IQueryOptions<ITableField> AddLogicalFilter(IEnumerable<FilterCondition<ITableField>> conditions, LogicalOperator or)
    {
        var materializedConditions = conditions?.ToList() ?? new List<FilterCondition<ITableField>>();

        if (!materializedConditions.Any())
        {
            return this;
        }

        if (or == LogicalOperator.Or)
        {
            Filters.Add(new(default!, FilterOperator.Equals, materializedConditions));
        }
        else
        {
            Filters.AddRange(materializedConditions);
        }
        return this;
    }
    public BaseQueryOptions<ITableField> SetOrdering(ITableField field, bool descending = false)
    {
        OrderBy = field;
        OrderDescending = descending;
        return this;
    }

    public BaseQueryOptions<ITableField> SetPagination(int limit, int offset = 0)
    {
        Limit = limit;
        Offset = offset;
        return this;
    }
    public BaseQueryOptions<ITableField> SelectFields(IEnumerable<ITableField> fields)
    {
        if (!CheckFields(fields))
            throw new ArgumentException("One or more invalid fields specified.");
        SelectedFields = fields;
        return this;
    }
    private bool CheckFields(IEnumerable<ITableField> fields)
    {
        return fields.All(f => Enum.IsDefined(typeof(ITableField), f));
    }   
    public BaseQueryOptions<ITableField> Reset()
    {
        Limit = null;
        Offset = null;
        OrderBy = default!;
        OrderDescending = false;
        SelectedFields = new List<ITableField>();
        Filters.Clear();
        return this;
    }
}
