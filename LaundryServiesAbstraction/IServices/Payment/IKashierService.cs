using LaundryShared.Common;
using LaundryShared.DTO.PaymentKashier;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Payment
{
    /// <summary>
    /// عقد الخدمة بتاعة Kashier
    /// بيتعامل مع كل عمليات الدفع والتحويل
    /// </summary>
    public interface IKashierService
    {
        // ============================================================
        // شحن محفظة العميل
        // ============================================================

        /// <summary>
        /// بيكريت URL لصفحة الدفع بتاعة Kashier عشان العميل يشحن محفظته
        /// </summary>
        Task<Result<KashierPaymentDto>> CreateWalletChargeAsync(
            string customerId, decimal amount);

        /// <summary>
        /// الدفع باستخدام محفظة التطبيق (الأرصدة المشحونة)
        /// </summary>
        Task<Result<bool>> CreateWalletPaymentAsync(
            string customerId, int orderId, decimal amount);

        /// <summary>
        /// استرجاع الرصيد للمحفظة في حالة إلغاء الأوردر المدفوع مقدماً
        /// </summary>
        Task<Result<bool>> RefundWalletPaymentAsync(
            string customerId, int orderId, decimal amount);

        // ============================================================
        // دفع الأوردر
        // ============================================================

        /// <summary>
        /// دفع بـ Mobile Wallet
        /// بيكريت URL لصفحة الدفع بتاعة Kashier
        /// </summary>
        Task<Result<KashierPaymentDto>> CreateMobileWalletPaymentAsync(
            string customerId, int orderId, decimal amount);

        /// <summary>
        /// دفع كاش عند الاستلام
        /// بيسجل الأوردر كـ Cash وبيحسب الدين على المغسلة
        /// </summary>
        Task<Result<bool>> CreateCashPaymentAsync(
            string customerId, int orderId, decimal amount);

        /// <summary>
        /// دفع بالفيزا/بطاقة الائتمان
        /// بيكريت URL لصفحة الدفع بتاعة Kashier
        /// </summary>
        Task<Result<KashierPaymentDto>> CreateCardPaymentAsync(
            string customerId, int orderId, decimal amount);

        /// <summary>
        /// دفع عمولة المنصة بواسطة صاحب المغسلة عبر Kashier
        /// </summary>
        Task<Result<KashierPaymentDto>> CreateCommissionPaymentAsync(
            string laundryAdminId, int laundryId, decimal amount, string currency = "EGP");

        // ============================================================
        // Webhook من Kashier
        // ============================================================

        /// <summary>
        /// بيستقبل الـ Callback من Kashier
        /// وبنتأكد من الـ Signature
        /// </summary>
        Task<Result<bool>> HandleWebhookAsync(string signature, KashierWebhookDto webhook);

        // ============================================================
        // Payout لصاحب المغسلة
        // ============================================================

        /// <summary>
        /// بيحول فلوس لصاحب المغسلة
        /// بيتنادى كل يوم خميس من الـ Hangfire
        /// </summary>
        Task<Result<bool>> ProcessPayoutAsync(int laundryWalletId);

        /// <summary>
        /// بيعالج تحديثات قاعدة البيانات عند رجوع العميل من بوابة الدفع عبر Callback
        /// </summary>
        Task<Result<bool>> ProcessCallbackPaymentAsync(string merchantOrderId, decimal amount, string status);
    }
}
