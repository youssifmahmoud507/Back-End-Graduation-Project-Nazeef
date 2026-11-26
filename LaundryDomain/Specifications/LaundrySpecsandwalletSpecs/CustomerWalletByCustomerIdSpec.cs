using LaundryDomain.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundrySpecsandwalletSpecs
{

    /// <summary>
    /// بتجيب محفظة العميل بالـ CustomerId
    /// </summary>
    public class CustomerWalletByCustomerIdSpec : BaseSpecification<CustomerWallet, int>
    {
        public CustomerWalletByCustomerIdSpec(string customerId)
            : base(w => w.CustomerId == customerId) { }
    }
}
