using System.Data;
using Dapper;

namespace ServicioReportes.Infrastructure.Shared.Persistence;

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string text when DateOnly.TryParse(text, out var parsed) => parsed,
            _ => throw new DataException($"No se pudo convertir el valor '{value}' a DateOnly.")
        };
    }
}

public static class DapperTypeHandlers
{
    private static int initialized;

    public static void Register()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 1)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }
}
