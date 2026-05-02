namespace Mercadito.Users.Api.Domain.Shared.Repository
{
    public interface ICrudRepository<TCreateModel, TUpdateModel, TReadModel, TId>
    {
        Task<TReadModel?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(TCreateModel entity, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(TUpdateModel entity, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);
    }
}
