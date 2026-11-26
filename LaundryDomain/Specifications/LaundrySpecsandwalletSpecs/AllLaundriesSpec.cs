using LaundryDomain.Models.LaundryModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundrySpecs
{
    /// <summary>
    /// بتجيب كل المغاسل في السيستم
    /// بتستخدم في لوحة تحكم الـ Super Admin بس
    /// </summary>
    public class AllLaundriesSpec : BaseSpecification<Laundry, int>
    {
        public AllLaundriesSpec()
            : base(l => true)
        {
            // بنرتب من الأحدث للأقدم
            AddOrderByDescending(l => l.CreatedAt);
        }
    }
}
