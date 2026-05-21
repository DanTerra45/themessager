using Application.Options;
using Domain.Database;
using Domain.Common;
using Domain.Entities;
using Domain.Dto.Response;
using Dapper;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User, int, UserFields, UserOptions, UserSchema>
    {
        public UserRepository(IDbConnectionFactory db, ILogger<BaseRepository<User, int, UserFields, UserOptions, UserSchema>> logger) : base(db, "users", logger) { }
    }
}