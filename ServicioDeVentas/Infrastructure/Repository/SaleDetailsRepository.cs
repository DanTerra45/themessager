using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Repository;
using Infrastructure.Database;

namespace Infrastructure.Repository
{
  public class SaleDetailRepository : BaseRepository<SaleDetails, int, SaleDetailFields, SaleDetailOptions, SaleDetailSchema>, ISaleDetailRepository
  {
    public SaleDetailRepository(
        IDbConnectionFactory db,
        ILogger<SaleDetailRepository> logger
    ) : base(db, "sale_details", logger)
    {
    }
    public async override Task<Result<SaleDetails>> GetByIdAsync(int id, SaleDetailOptions? options)
    {
      var saleDetailOptions = options ?? new SaleDetailOptions();
      saleDetailOptions.AddFilter(SaleDetailFields.Id, FilterOperator.Equals, id);
      var result = await base.GetOneAsync(saleDetailOptions);
      return result;
    }
  }
}
