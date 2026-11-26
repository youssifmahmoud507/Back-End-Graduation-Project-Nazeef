using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.SystemModules
{
    public class Review : BaseEntity<Review, int>
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        // FKs
        [Required]
        public string CustomerId { get; set; }

        [Required]
        public int LaundryId { get; set; }

        [Required]
        public int OrderId { get; set; }
        public SentimentType? Sentiment { get; set; }// "Positive" / "Negative" / "Neutral"
        public float? SentimentScore { get; set; }   // 0.0 - 1.0
        public string? SentimentSummary { get; set; }

        // Navigation Properties
        public ApplicationUser Customer { get; set; }
        public Laundry Laundry { get; set; }
        public Order Order { get; set; }
        public string? Tags { get; set; } // JSON string

    }
}
