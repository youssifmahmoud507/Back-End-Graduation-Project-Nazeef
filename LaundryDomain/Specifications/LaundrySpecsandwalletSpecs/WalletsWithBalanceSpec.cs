using LaundryDomain.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundrySpecsandwalletSpecs
{
    /// <summary>
    /// بتجيب كل المغاسل اللي عندها رصيد متاح للتحويل
    /// بتستخدم في الـ Weekly Payout كل يوم خميس
    /// </summary>
    public class WalletsWithBalanceSpec : BaseSpecification<LaundryWallet, int>
    {
        public WalletsWithBalanceSpec()
            : base(w => w.AvailableBalance > 0 &&
                        w.Status == LaundryShared.Enums.WalletEnums.LaundryWalletStatus.Active)
        {
            // بنجيب بيانات المغسلة عشان نعرف الـ AdminId
            AddInclude(w => w.Laundry);
        }
    }
}
