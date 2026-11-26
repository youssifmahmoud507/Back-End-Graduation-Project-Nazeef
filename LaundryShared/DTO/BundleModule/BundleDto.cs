using System;
using System.Collections.Generic;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal BundlePrice { get; set; }
        public decimal OriginalPrice { get; set; }

        public decimal SavingsAmount => OriginalPrice - BundlePrice;
        public decimal SavingsPercentage => OriginalPrice > 0 
            ? Math.Round((SavingsAmount / OriginalPrice) * 100, 1) 
            : 0;

        public BundleStatus Status { get; set; }
        public BundleOwnerType OwnerType { get; set; }
        public int? LaundryId { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public List<BundleItemDto> BundleItems { get; set; } = new List<BundleItemDto>();
        public required string CreatedByAdminId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
