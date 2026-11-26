namespace LaundryShared.DTO.Reviews
{
    public class OrderReviewCheckDto
    {
        public bool HasRating { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }
}
