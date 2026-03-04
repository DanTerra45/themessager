using System;
using System.Data;
using Dapper;

namespace Mercadito;

public class GuidBinaryTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        if (value is byte[] bytes && bytes.Length == 16)
        {
            return new Guid(bytes);
        }
        if (value is string str)
        {
            return Guid.Parse(str);
        }
        throw new ArgumentException($"Cannot convert {value?.GetType().Name} to Guid");
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToByteArray();
        parameter.DbType = DbType.Binary;
    }
}