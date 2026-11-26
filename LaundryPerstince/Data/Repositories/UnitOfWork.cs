using LaundryDomain.IRepositories;
using LaundryDomain.Models.Common;
using LaundryPerstince.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NadeefDbContext _context;
        private readonly Dictionary<string, object> _repositories;

        public UnitOfWork(NadeefDbContext context)
        {
            _context = context;
            _repositories = new Dictionary<string, object>();
        }

        public IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>()
            where TEntity : BaseEntity<TEntity, TKey>
        {
            var key = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(key))
            {
                var repo = new GenericRepository<TEntity, TKey>(_context);
                _repositories.Add(key, repo);
            }

            return (IGenericRepository<TEntity, TKey>)_repositories[key];
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
