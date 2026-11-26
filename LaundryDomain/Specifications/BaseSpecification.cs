using LaundryDomain.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications
{
    public class BaseSpecification<TEntity, TKey> : ISpecification<TEntity, TKey>
        where TEntity : BaseEntity<TEntity, TKey>
    {
        // Criteria بتقرأ من _criteria
        private Expression<Func<TEntity, bool>> _criteria;
        public Expression<Func<TEntity, bool>> Criteria => _criteria;

        public List<Expression<Func<TEntity, object>>> Includes { get; }
            = new List<Expression<Func<TEntity, object>>>();

        public List<string> IncludeStrings { get; }
            = new List<string>();

        public Expression<Func<TEntity, object>> OrderBy { get; private set; }
        public Expression<Func<TEntity, object>> OrderByDescending { get; private set; }
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        // Constructor فاضي
        public BaseSpecification()
        {
        }

        // Constructor بـ Criteria مباشرة
        public BaseSpecification(Expression<Func<TEntity, bool>> criteria)
        {
            _criteria = criteria;
        }

        protected void AddCriteria(Expression<Func<TEntity, bool>> criteriaExpression)
        {
            _criteria = criteriaExpression;
        }

        protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        protected void AddOrderBy(Expression<Func<TEntity, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected void AddOrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}
