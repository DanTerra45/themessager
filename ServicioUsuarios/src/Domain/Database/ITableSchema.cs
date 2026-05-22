using Application.Options;

namespace Domain.Database
{
    public interface ITableSchema<TFields>
        where TFields : Enum
    {
        string GetAll();
        string Get(TFields field, SqlAction action);
    }
}