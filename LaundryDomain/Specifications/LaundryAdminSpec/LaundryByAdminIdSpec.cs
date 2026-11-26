using LaundryDomain.Models.LaundryModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundryAdminIdSpec
{
    /// <summary>
    /// بتجيب المغسلة بتاعة الأدمن ده بس
    /// بتستخدم الـ AdminId اللي جاي من الـ JWT Token
    /// </summary>
    public class LaundryByAdminIdSpec : BaseSpecification<Laundry, int>
    {
        public LaundryByAdminIdSpec(string adminId)
            : base(l => l.AdminId == adminId)
        {
            // بنجيب الخدمات مع المغسلة عشان نعرض كل حاجة
            AddInclude(l => l.Services);
        }
    }
}
