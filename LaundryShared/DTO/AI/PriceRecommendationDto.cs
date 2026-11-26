namespace LaundryShared.DTO.AI
{
    public class PriceRecommendationDto
    {
        public decimal SuggestedMinPrice { get; set; }
        public decimal SuggestedMaxPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public string? Reasoning { get; set; }
    }
}
