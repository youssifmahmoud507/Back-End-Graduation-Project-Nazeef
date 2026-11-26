using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Reviews
{
    public class UpdateReviewRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
