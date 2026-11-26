using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.AI
{
    public class PriceRecommendationRequest
    {
        public string ServiceName { get; set; }
        public ServiceCategory Category { get; set; }
    }
}
