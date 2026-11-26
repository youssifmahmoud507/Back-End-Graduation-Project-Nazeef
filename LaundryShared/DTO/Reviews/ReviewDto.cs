using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Reviews
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int LaundryId { get; set; }
        public string LaundryName { get; set; }
        public string CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public SentimentType? Sentiment { get; set; }
        public float? SentimentScore { get; set; }
        public string? SentimentSummary { get; set; }
    }
}
