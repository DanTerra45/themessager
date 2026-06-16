using System.Data;
using Domain.Database;

namespace Infrastructure.Database
{
  public class UnitOfWork : IUnitOfWork
  {
    private readonly IDbConnectionFactory _db;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(IDbConnectionFactory db)
    {
      _db = db;
    }

    public IDbConnection Connection => _connection ?? throw new InvalidOperationException("Connection not initialized. Call BeginAsync().");

    public IDbTransaction? Transaction => _transaction;

    public async Task BeginAsync()
    {
      if (_connection != null) return;
      _connection = await _db.CreateConnectionAsync();
      _transaction = _connection.BeginTransaction();
    }

    public async Task CommitAsync()
    {
      if (_transaction == null) throw new InvalidOperationException("No active transaction to commit.");
      _transaction.Commit();
      await DisposeAsync();
    }

    public async Task RollbackAsync()
    {
      try
      {
        _transaction?.Rollback();
      }
      finally
      {
        await DisposeAsync();
      }
    }

    public async ValueTask DisposeAsync()
    {
      try
      {
        _transaction?.Dispose();
      }
      catch { }
      try
      {
        if (_connection != null)
        {
          try { _connection.Close(); } catch { }
          _connection.Dispose();
        }
      }
      catch { }
      _transaction = null;
      _connection = null;
      await Task.CompletedTask;
    }
  }
}
