using Application.Options;
using Domain.Database;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Dto.Response;
using Dapper;
using Microsoft.Extensions.Logging;
using Infrastructure.Database;
using Domain.Database.Fields;
namespace Infrastructure.Repository
{
    public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken, int, PasswordResetTokenFields, PasswordResetTokenOptions, PasswordResetTokenSchema>
    {
        private readonly ILogger<PasswordResetTokenRepository> _logger;
        public PasswordResetTokenRepository(IDbConnectionFactory db, ILogger<BaseRepository<PasswordResetToken, int, PasswordResetTokenFields, PasswordResetTokenOptions, PasswordResetTokenSchema>> logger, ILogger<PasswordResetTokenRepository> logger2) : base(db, "password_reset_token", logger)
        {
            _logger = logger2;
        }
    }
}