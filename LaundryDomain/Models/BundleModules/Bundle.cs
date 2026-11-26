using System;
using System.Collections.Generic;
using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;


namespace LaundryDomain.Models.BundleModules
{
    public class Bundle : BaseEntity<Bundle, Guid>
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal BundlePrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public BundleStatus Status { get; set; }
        public BundleOwnerType OwnerType { get; set; }
        public required string CreatedByAdminId { get; set; }
        public int? LaundryId { get; set; } // Matches Laundry Entity Id typing
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();
        public ICollection<BundleApprovalRequest> ApprovalRequests { get; set; } = new List<BundleApprovalRequest>();
        public ApplicationUser CreatedByAdmin { get; set; } = null!;
        public Laundry? Laundry { get; set; }
        public ICollection<BundleOrderMetadata> BundleOrderMetadatas { get; set; } = new List<BundleOrderMetadata>();
    }
}