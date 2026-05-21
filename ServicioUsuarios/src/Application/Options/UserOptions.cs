using Domain.Database;

namespace Application.Options;

public class UserOptions : IQueryOptions<UserFields>
{
    
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public UserFields OrderBy { get; set; }
    public bool OrderDescending { get; set; } = false;
    public IEnumerable<UserFields> SelectedFields { get; set; } = new List<UserFields>();
    public List<FilterCondition<UserFields>> Filters { get; set; } = new();

    public IQueryOptions<UserFields> AddFilter(UserFields field, FilterOperator op, object? value)
    {
        Filters.Add(new(field, op, value));
        return this;
    }
    public UserOptions SetOrdering(UserFields field, bool descending = false)
    {
        OrderBy = field;
        OrderDescending = descending;
        return this;
    }

    public UserOptions SetPagination(int limit, int offset = 0)
    {
        Limit = limit;
        Offset = offset;
        return this;
    }
    public UserOptions SelectFields(IEnumerable<UserFields> fields)
    {
        if (!CheckFields(fields))
            throw new ArgumentException("One or more invalid fields specified.");
        SelectedFields = fields;
        return this;
    }
    private bool CheckFields(IEnumerable<UserFields> fields)
    {
        return fields.All(f => Enum.IsDefined(typeof(UserFields), f));
    }   
    public UserOptions Reset()
    {
        Limit = null;
        Offset = null;
        OrderBy = default;
        OrderDescending = false;
        SelectedFields = new List<UserFields>();
        Filters.Clear();
        return this;
    }
}
