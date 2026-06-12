using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;

namespace Domain.Repository
{
    public interface ISaleRepository : ICrudRepository<SaleWithDetails, int, SaleFields, SaleOptions>
    {
    }
}