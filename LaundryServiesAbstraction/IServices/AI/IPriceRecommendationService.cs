using LaundryShared.DTO.AI;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryServiesAbstraction.IServices.AI
{
    public interface IPriceRecommendationService
    {
        Task<PriceRecommendationDto?> GetPriceRecommendationAsync(string serviceName, ServiceCategory category);
    }
}
