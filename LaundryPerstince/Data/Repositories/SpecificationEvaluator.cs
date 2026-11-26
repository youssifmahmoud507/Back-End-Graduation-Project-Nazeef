using LaundryDomain.Models.Common;
using LaundryDomain.Specifications;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Repositories
{
    public static class SpecificationEvaluator<TEntity, TKey>
        where TEntity : BaseEntity<TEntity, TKey>
    {
        public static IQueryable<TEntity> GetQuery(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity, TKey> spec)
        {
            var query = inputQuery;

            // Apply Criteria (Where)
            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            // Apply Includes (expression-based)
            query = spec.Includes
                        .Aggregate(query, (current, include) =>
                            current.Include(include));

            // Apply string-based Includes (supports ThenInclude via "Navigation.SubNavigation")
            query = spec.IncludeStrings
                        .Aggregate(query, (current, include) =>
                            current.Include(include));

            // Apply OrderBy
            if (spec.OrderBy != null)
                query = query.OrderBy(spec.OrderBy);

            // Apply OrderByDescending
            if (spec.OrderByDescending != null)
                query = query.OrderByDescending(spec.OrderByDescending);

            // Apply Pagination
            if (spec.IsPagingEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            return query;
        }
    }
}
