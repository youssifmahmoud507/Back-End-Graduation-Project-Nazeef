using LaundryDomain.Models.Common;

namespace LaundryDomain.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>()
            where TEntity : BaseEntity<TEntity, TKey>;

        Task<int> CompleteAsync();
    }
}
