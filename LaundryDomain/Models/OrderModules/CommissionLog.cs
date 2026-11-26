using LaundryDomain.Models.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaundryDomain.Models.OrderModules
{
    public class CommissionLog : BaseEntity<CommissionLog, int>
    {
        public int OrderId { get; set; }
        public int LaundryId { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OrderAmount { get; set; }
        
        [Column(TypeName = "decimal(5, 2)")]
        public decimal CommissionPercentage { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CommissionAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}