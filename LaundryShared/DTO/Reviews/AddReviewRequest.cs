using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.Reviews
{
    public class AddReviewRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int LaundryId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
