using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class ServiceCatalogItemDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public ServiceCategory Category { get; set; }

        public bool IsAdded { get; set; }
        public int? ExistingServiceId { get; set; }
        public decimal? CurrentPrice { get; set; }
        public bool? IsAvailable { get; set; }

        public decimal? SuggestedMinPrice { get; set; }
        public decimal? SuggestedMaxPrice { get; set; }
        public decimal? RecommendedPrice { get; set; }
        public string? SuggestionReasoning { get; set; }
    }
}
