using LaundryDomain.Models.Wallet;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.WalletSpecs
{
    public class LaundryPayoutProfileByLaundryIdSpec : BaseSpecification<LaundryPayoutProfile, int>
    {
        public LaundryPayoutProfileByLaundryIdSpec(int laundryId)
            : base(p => p.LaundryId == laundryId)
        {
        }
    }
}
