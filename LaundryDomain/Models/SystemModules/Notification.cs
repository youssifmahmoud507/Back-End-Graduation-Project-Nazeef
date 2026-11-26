using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.SystemModules
{
    public class Notification : BaseEntity<Notification, int>
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [Required]
        [MaxLength(50)]
        public NotificationType Type { get; set; }

        [MaxLength(50)]
        public string? TargetRole { get; set; }

        public bool IsRead { get; set; } = false;

        // FK
        [Required]
        public string UserId { get; set; }

        // Navigation Property
        public ApplicationUser User { get; set; }
    }
}
