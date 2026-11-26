using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryShared.Enums.WalletEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.Wallet
{
    /// <summary>
    /// محفظة المغسلة
    /// بتتملى من الأونلاين بيمنتس
    /// وبتتخصم منها العمولات
    /// </summary>
    public class LaundryWallet : BaseEntity<LaundryWallet, int>
    {
        /// <summary>الـ ID بتاع المغسلة</summary>
        public int LaundryId { get; set; }

        /// <summary>إجمالي الأرباح من أول ما اشتغلت</summary>
        public decimal TotalEarnings { get; set; } = 0;

        /// <summary>الرصيد المتاح للتحويل ليه يوم الخميس</summary>
        public decimal AvailableBalance { get; set; } = 0;

        /// <summary>
        /// العمولات المتأخرة من الكاش
        /// بتتخصم من أول أونلاين بيمنت جاي
        /// </summary>
        public decimal PendingCommission { get; set; } = 0;

        /// <summary>
        /// سقف المديونية = 200 جنيه
        /// لو PendingCommission وصلت ده
        /// المغسلة بتتوقف أوتوماتيك
        /// </summary>
        public decimal DebtCeiling { get; set; } = 200;

        /// <summary>Active / Suspended</summary>
        public LaundryWalletStatus Status { get; set; } = LaundryWalletStatus.Active;

        /// <summary>تاريخ آخر تحويل ليه</summary>
        public DateTime? LastPayoutDate { get; set; }

        // Navigation
        public Laundry Laundry { get; set; } = null!;
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
