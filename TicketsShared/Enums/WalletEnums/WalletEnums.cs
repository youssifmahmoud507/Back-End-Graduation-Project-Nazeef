using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.Enums.WalletEnums
{
    /// <summary>حالة محفظة المغسلة</summary>
    public enum LaundryWalletStatus
    {
        Active = 1,    // شغالة وبتستقبل
        Suspended = 2  // موقوفة بسبب الديون
    }

    /// <summary>نوع العملية</summary>
    public enum TransactionType
    {
        Credit = 1, // فلوس دخلت
        Debit = 2   // فلوس خرجت
    }

    /// <summary>مصدر العملية</summary>
    public enum TransactionSource
    {
        WalletCharge = 1,  // العميل شحن المحفظة
        WalletPayment = 2, // العميل دفع من المحفظة
        MobileWallet = 3,  // دفع بفودافون كاش / اتصالات
        CashPayment = 4,   // دفع كاش عند الاستلام
        Payout = 5,        // تحويل لصاحب المغسلة
        Refund = 6         // استرداد
    }

    /// <summary>حالة العملية</summary>
    public enum TransactionStatus
    {
        Pending = 1,   // لسه مكتملتش
        Completed = 2, // اكتملت
        Failed = 3     // فشلت
    }

    /// <summary>طريقة دفع الأوردر</summary>
    public enum OrderPaymentMethod
    {
        Wallet = 1,       // من محفظة التطبيق
        MobileWallet = 2, // فودافون كاش / اتصالات
        Cash = 3          // كاش عند الاستلام
    }

    /// <summary>طريقة تحويل مستحقات المغسلة</summary>
    public enum PayoutTransferMethod
    {
        BankAccount = 1,
        MobileWallet = 2,
        Card = 3,
        OctoCard = 4
    }

    /// <summary>نوع التحويل (مثال: Standard، Instant)</summary>
    public enum PayoutTransferType
    {
        Standard = 1,
        Instant = 2
    }
}
