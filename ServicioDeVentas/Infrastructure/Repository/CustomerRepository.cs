using Application.Options;
using Dapper;
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
        
    }
}