using LaundryDomain.Models.Common;
using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.WalletEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.Wallet
{
    /// <summary>
    /// سجل كل العمليات اللي بتحصل على المحافظ
    /// سواء شحن أو دفع أو تحويل
    /// </summary>
    public class WalletTransaction : BaseEntity<WalletTransaction, int>
    {
        /// <summary>محفظة المغسلة لو العملية بتاعتها</summary>
        public int? LaundryWalletId { get; set; }

        /// <summary>محفظة العميل لو العملية بتاعته</summary>
        public int? CustomerWalletId { get; set; }

        /// <summary>الأوردر المرتبط بالعملية دي</summary>
        public int? OrderId { get; set; }

        /// <summary>المبلغ الكلي للعملية</summary>
        public decimal Amount { get; set; }

        /// <summary>نسبة العمولة بتاعة نضيف</summary>
        public decimal CommissionRate { get; set; }

        /// <summary>مبلغ العمولة بتاعة نضيف</summary>
        public decimal CommissionAmount { get; set; }

        /// <summary>Credit = فلوس دخلت / Debit = فلوس خرجت</summary>
        public TransactionType Type { get; set; }

        /// <summary>مصدر العملية (شحن / دفع / كاش / تحويل)</summary>
        public TransactionSource Source { get; set; }

        /// <summary>Paymob Transaction ID للرجوع ليه لو في مشكلة</summary>
        public string? PaymobTransactionId { get; set; }

        /// <summary>Pending / Completed / Failed</summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // Navigation
        public LaundryWallet? LaundryWallet { get; set; }
        public CustomerWallet? CustomerWallet { get; set; }
        public Order? Order { get; set; }
    }
}
