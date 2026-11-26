using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using System;

namespace LaundryDomain.Models.Wallet
{
    public class LaundryPayout : BaseEntity<LaundryPayout, int>
    {
        public int LaundryId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Completed";
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }

        public Laundry Laundry { get; set; } = null!;
    }
}
