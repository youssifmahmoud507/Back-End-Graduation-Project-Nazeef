using LaundryShared.DTO.AI;
using LaundryShared.DTO.Analytics;

namespace LaundryServiesAbstraction.IServices.AI
{
    public interface IDemandForecastService
    {
        Task<DemandForecastDto?> GetDemandForecastAsync(LaundryAnalyticsDto analytics);
    }
}
