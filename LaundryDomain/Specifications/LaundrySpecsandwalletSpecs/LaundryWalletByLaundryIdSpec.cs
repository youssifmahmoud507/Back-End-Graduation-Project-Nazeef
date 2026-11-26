using LaundryDomain.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundrySpecsandwalletSpecs
{

    /// <summary>
    /// بتجيب محفظة المغسلة بالـ LaundryId
    /// </summary>
    public class LaundryWalletByLaundryIdSpec : BaseSpecification<LaundryWallet, int>
    {
        public LaundryWalletByLaundryIdSpec(int laundryId)
            : base(w => w.LaundryId == laundryId) { }
    }
}
