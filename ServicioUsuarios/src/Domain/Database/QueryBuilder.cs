using System.Text;
using Application.Options;
using Dapper;

namespace Domain.Database
{
    public class QueryBuilder<TOptions, TFields>
        where TOptions : IQueryOptions<TFields>
        where TFields : Enum
    {
        private readonly StringBuilder _sb = new();
        private  DynamicParameters _parameters = new();
        private readonly string _tableName;
        private readonly ITableSchema<TFields> _schema;
        private int _paramCounter = 0;
        public QueryBuilder(string tableName, ITableSchema<TFields> schema)
        {
            _tableName = tableName;
            _schema = schema;
        }

        public QueryBuilder<TOptions, TFields> Select(TOptions options)
        {
            var fields = options?.SelectedFields != null && options.SelectedFields.Any()
                ? string.Join(", ", options.SelectedFields.Select(f => _schema.Get(f)))
                : _schema.GetAll();

            _sb.Append($"SELECT {fields} FROM {_tableName}");
            return this;
        }

        public QueryBuilder<TOptions, TFields> Where(TOptions options)
        {
            _sb.Append(" WHERE 1=1");

            if (options?.Filters == null || !options.Filters.Any())
                return this;

            foreach (var filter in options.Filters)
            {
                var column = _schema.Get(filter.Field);
                var baseName = filter.Field.ToString();
                string paramName() => $"{baseName}_{_paramCounter++}";

                switch (filter.Operator)
                {
                    case FilterOperator.Equals:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} = @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.NotEquals:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} <> @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.GreaterThan:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} > @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.GreaterThanOrEqual:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} >= @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.LessThan:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} < @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.LessThanOrEqual:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} <= @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.Contains:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} LIKE @{p}");
                            _parameters.Add(p, $"%{filter.Value}%");
                            break;
                        }
                    case FilterOperator.StartsWith:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} LIKE @{p}");
                            _parameters.Add(p, $"{filter.Value}%");
                            break;
                        }
                    case FilterOperator.EndsWith:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} LIKE @{p}");
                            _parameters.Add(p, $"%{filter.Value}");
                            break;
                        }
                    case FilterOperator.Like:
                        {
                            var p = paramName();
                            _sb.Append($" AND {column} LIKE @{p}");
                            _parameters.Add(p, filter.Value);
                            break;
                        }
                    case FilterOperator.ILike:
                        {
                            var p = paramName();
                            _sb.Append($" AND LOWER({column}) LIKE LOWER(@{p})");
                            var v = filter.Value?.ToString();
                            _parameters.Add(p, $"%{v}%");
                            break;
                        }
                    case FilterOperator.Between:
                        {
                            if (filter.Value is Tuple<object, object> range)
                            {
                                var pStart = paramName();
                                var pEnd = paramName();
                                _sb.Append($" AND {column} BETWEEN @{pStart} AND @{pEnd}");
                                _parameters.Add(pStart, range.Item1);
                                _parameters.Add(pEnd, range.Item2);
                            }
                            else
                            {
                                throw new ArgumentException("Value for 'Between' must be a Tuple<object, object>.");
                            }
                            break;
                        }
                    case FilterOperator.IsNull:
                        _sb.Append($" AND {column} IS NULL");
                        break;
                    case FilterOperator.IsNotNull:
                        _sb.Append($" AND {column} IS NOT NULL");
                        break;
                    default:
                        throw new NotSupportedException($"Operator {filter.Operator} not supported.");
                }
            }
            return this;
        }
        public QueryBuilder<TOptions, TFields> OrderBy(TOptions options)
        {
            if (!EqualityComparer<TFields>.Default.Equals(options.OrderBy, default))
            {
                var column = _schema.Get(options.OrderBy);
                _sb.Append($" ORDER BY {column} {(options.OrderDescending ? "DESC" : "ASC")}");
            }
            return this;
        }
        public QueryBuilder<TOptions, TFields> Paginate(TOptions options)
        {
            if (options.Limit is not null)
                _sb.Append($" LIMIT {options.Limit} OFFSET {options.Offset ?? 0}");
            return this;
        }
        public QueryBuilder<TOptions, TFields> BuildFromOptions(TOptions options)
        {
            return Select(options).Where(options).OrderBy(options).Paginate(options);
        }
        public QueryBuilder<TOptions, TFields> Reset()
        {
            _sb.Clear();
            _parameters = new DynamicParameters();
            _paramCounter = 0;
            return this;
        }
        public (string Sql, DynamicParameters Parameters) Query(string sql, DynamicParameters parameters)
        {
            _sb.Append(sql);
            foreach (var p in parameters.ParameterNames)
            {
                _parameters.Add(p, parameters.Get<dynamic>(p));
            }
            return (_sb.ToString(), _parameters);
        }
        public (string Sql, DynamicParameters Parameters) Build()
        {
            return (_sb.ToString(), _parameters);
        }
    }
}