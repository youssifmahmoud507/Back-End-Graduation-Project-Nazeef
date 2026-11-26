using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaundryDomain.Models.Wallet
{
    public class CommissionLog : BaseEntity<CommissionLog, int>
    {

        [Required]
        [ForeignKey("Laundry")]
        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; }

        [ForeignKey("Order")]
        public int? OrderId { get; set; }
        public Order Order { get; set; }

        public decimal OrderAmount { get; set; }
        public decimal CommissionPercentage { get; set; }
        public decimal CommissionAmount { get; set; }

    }
}
