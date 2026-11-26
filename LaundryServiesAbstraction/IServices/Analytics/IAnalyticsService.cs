using LaundryShared.Common;
using LaundryShared.DTO.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Analytics
{
    /// <summary>
    /// عقد الخدمة بتاعة الإحصائيات والتقارير
    /// فيه تقريرين - واحد لصاحب المغسلة وواحد للـ Super Admin
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// تقرير صاحب المغسلة
        /// بيشوف أرباحه وطلباته وأكتر خدمة مطلوبة
        /// </summary>
        Task<Result<LaundryAnalyticsDto>> GetLaundryAnalyticsAsync(string adminId);

        /// <summary>
        /// تقرير الـ Super Admin
        /// بيشوف إحصائيات السيستم كله
        /// </summary>4
        Task<Result<SystemAnalyticsDto>> GetSystemAnalyticsAsync();
    }
}