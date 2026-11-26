using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.SystemModules
{
    public class Complaint : BaseEntity<Complaint, int>
    {
        [Required]
        [MaxLength(1000)]
        public string Details { get; set; }

        [Required]
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

        [Required]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int LaundryId { get; set; }

        [Required]
        public string LaundryAdminId { get; set; }
        public ApplicationUser LaundryAdmin { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
