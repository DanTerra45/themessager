using System.Text;
using Dapper;
using Domain.Database;

namespace Infrastructure.Database
{
    public class QueryBuilder<TFields> where TFields : Enum
    {
        private readonly Func<TFields, string> _getColumnName;
        public StringBuilder Sql { get; private set; }
        public DynamicParameters Parameters { get; private set; }

        public QueryBuilder(Func<TFields, string> getColumnName)
        {
            _getColumnName = getColumnName;
            Sql = new StringBuilder();
            Parameters = new DynamicParameters();
        }

        public void Select(IEnumerable<TFields> fields, string tableName)
        {
            if (fields == null || !fields.Any())
            {
                Sql.Append($"SELECT * FROM {tableName}");
            }
            else
            {
                var columnNames = fields.Select(f => _getColumnName(f));
                Sql.Append($"SELECT {string.Join(", ", columnNames)} FROM {tableName}");
            }
        }
        public void Where(IQueryOptions<TFields> options, string tableName)
        {
            if (options.Filters == null || !options.Filters.Any())
                return;

            Sql.Append(" WHERE ");
            var conditions = new List<string>();
            int paramIndex = 0;

            foreach (var filter in options.Filters)
            {
                var columnName = _getColumnName(filter.Field);
                var paramName = $"@param{paramIndex++}";

                string condition = filter.Operator switch
                {
                    FilterOperator.Equals => $"{columnName} = {paramName}",
                    FilterOperator.NotEquals => $"{columnName} <> {paramName}",
                    FilterOperator.GreaterThan => $"{columnName} > {paramName}",
                    FilterOperator.LessThan => $"{columnName} < {paramName}",
                    FilterOperator.GreaterOrEqual => $"{columnName} >= {paramName}",
                    FilterOperator.LessOrEqual => $"{columnName} <= {paramName}",
                    FilterOperator.Contains => $"{columnName} LIKE CONCAT('%', {paramName}, '%')",
                    _ => throw new NotSupportedException($"Unsupported operator: {filter.Operator}")
                };

                conditions.Add(condition);
                Parameters.Add(paramName, filter.Value);
            }

            Sql.Append(string.Join(" AND ", conditions));
        }
    }
}
