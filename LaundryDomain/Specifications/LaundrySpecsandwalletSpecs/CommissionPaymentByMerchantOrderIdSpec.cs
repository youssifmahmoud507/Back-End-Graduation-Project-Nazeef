using LaundryDomain.Models.Wallet;

namespace LaundryDomain.Specifications.LaundrySpecsandwalletSpecs
{
    public class CommissionPaymentByMerchantOrderIdSpec : BaseSpecification<CommissionPayment, int>
    {
        public CommissionPaymentByMerchantOrderIdSpec(string merchantOrderId)
            : base(cp => cp.MerchantOrderId == merchantOrderId)
        {
        }
    }
}
