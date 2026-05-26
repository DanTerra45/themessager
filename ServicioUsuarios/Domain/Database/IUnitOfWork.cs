using System.Data;

namespace Domain.Database
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
        Task BeginAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
