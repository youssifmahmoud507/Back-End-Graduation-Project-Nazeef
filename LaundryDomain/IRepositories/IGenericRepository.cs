using LaundryDomain.Models.Common;
using LaundryDomain.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.IRepositories
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TEntity, TKey>
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<IReadOnlyList<TEntity>> GetAllWithSpecAsync(ISpecification<TEntity, TKey> spec);
        Task<TEntity?> GetEntityWithSpecAsync(ISpecification<TEntity, TKey> spec);
        Task<int> CountAsync(ISpecification<TEntity, TKey> spec);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
    }
}
