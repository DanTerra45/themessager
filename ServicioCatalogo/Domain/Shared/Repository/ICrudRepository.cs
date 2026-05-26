namespace ServicioCatalogo.Domain.Shared.Repository
{
    public interface ICrudRepository<TCreateModel, TUpdateModel, TReadModel, TId>
    {
        Task<TReadModel?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(TCreateModel entity, long actorUserId, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(TUpdateModel entity, long actorUserId, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(TId id, long actorUserId, CancellationToken cancellationToken = default);
    }
}

