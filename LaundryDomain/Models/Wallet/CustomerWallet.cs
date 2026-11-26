using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LaundryDomain.Models.Wallet
{
    /// <summary>
    /// محفظة العميل جوه التطبيق
    /// بيشحنها بـ Paymob ويدفع منها
    /// </summary>
    public class CustomerWallet : BaseEntity<CustomerWallet, int>
    {
        /// <summary>الـ ID بتاع العميل</summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>الرصيد المتاح للدفع</summary>
        [Column(TypeName = "decimal(18, 2)")]
        [ConcurrencyCheck]
        public decimal Balance { get; set; } = 0;

        /// <summary>إجمالي اللي شحنه من أول ما فتح الأكونت</summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalCharged { get; set; } = 0;

        /// <summary>المحفظة شغالة ولا موقوفة</summary>
        public bool IsActive { get; set; } = true;

        // Navigation
        public ApplicationUser Customer { get; set; } = null!;
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
