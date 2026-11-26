using LaundryShared.Common;
using LaundryShared.DTO.Fraud;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryServiesAbstraction.IServices.Fraud
{
    /// <summary>
    /// عقد الخدمة بتاعة كشف الغش والعمليات المشبوهة
    /// بيستخدم Cerebras AI عشان يحلل السلوك
    /// </summary>
    public interface IFraudService
    {
        /// <summary>
        /// بيحلل عملية معينة ويشوف هي مشبوهة ولا لا
        /// </summary>
        Task<Result<FraudResultDto>> AnalyzeActionAsync(FraudDetectionDto request);

        /// <summary>
        /// بيحلل سلوك يوزر معين على مدار آخر 24 ساعة
        /// </summary>
        Task<Result<FraudResultDto>> AnalyzeUserBehaviorAsync(string userId);

        /// <summary>
        /// بيحلل عمليات الدفع المشبوهة
        /// </summary>
        Task<Result<FraudResultDto>> AnalyzePaymentAsync(
            string userId, decimal amount, string ipAddress);

        /// <summary>
        /// بيجيب كل التنبيهات المعلقة للمدير
        /// </summary>
        Task<Result<IEnumerable<SuspiciousActivityDto>>> GetPendingAlertsAsync();

        /// <summary>
        /// بيراجع تنبيه معين (حظر او رفض)
        /// </summary>
        Task<Result<bool>> ReviewAlertAsync(int alertId, bool approveBan);
    }
}
