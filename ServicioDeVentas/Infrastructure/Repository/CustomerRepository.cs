using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Repository;
using Infrastructure.Database;

namespace Infrastructure.Repository
{
  public class CustomerRepository : BaseRepository<Customer, int, CustomerFields, CustomerOptions, CustomerSchema>, ICustomerRepository
  {
    public CustomerRepository(
        IDbConnectionFactory db,
        ILogger<CustomerRepository> logger
    ) : base(db, "customer", logger)
    {
    }
    public async override Task<Result<Customer>> GetByIdAsync(int id, CustomerOptions? options)
    {
      var customerOptions = options ?? new CustomerOptions();
      customerOptions.AddFilter(CustomerFields.Id, FilterOperator.Equals, id);
      var result = await base.GetOneAsync(customerOptions);
      return result;
    }
  }
}
