using LaundryShared.Common;
using LaundryShared.DTO.Courier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Courier
{
    public interface ICourierService
    {
        // 1. View assigned orders
        Task<Pagination<CourierOrderDto>> GetAssignedOrdersAsync(
            string courierId, CourierOrderParams orderParams);

        // 1.5. View available (unassigned) orders in courier's laundry
        Task<Result<Pagination<CourierOrderDto>>> GetAvailableOrdersAsync(
            string courierId, int pageIndex, int pageSize);

        // 2. Accept an order
        Task<Result<CourierOrderDto>> AcceptOrderAsync(string courierId, int orderId);

        // 3. Update location
        Task<Result<UpdateLocationResponse>> UpdateLocationAsync(
            string courierId, UpdateLocationRequest request);

        // 4. Confirm pickup from laundry
        Task<Result<CourierOrderDto>> ConfirmPickupAsync(
            string courierId, int orderId, ConfirmPickupRequest request);

        // 5. Confirm delivery to customer
        Task<Result<CourierOrderDto>> ConfirmDeliveryAsync(
            string courierId, int orderId, ConfirmDeliveryRequest request);

        // 6. Cancel order
        Task<Result<bool>> CancelOrderAsync(
            string courierId, int orderId, CourierCancelOrderRequest request);

        // 7. Get Profile (App Requirement)
        Task<Result<CourierProfileResponseDto>> GetAppProfileAsync(string courierId);

        // 8. Update Status (App Requirement)
        Task<Result<bool>> UpdateStatusAsync(string courierId, UpdateCourierStatusRequestDto request);

        // 9. Get Today's Stats (App Requirement)
        Task<Result<CourierStatsResponseDto>> GetTodaysStatsAsync(string courierId);

        // 10. Get Active Run (App Requirement)
        Task<Result<CourierActiveRunResponseDto>> GetActiveRunAsync(string courierId);

        // 11. Deliver Stop in Active Run (App Requirement)
        Task<Result<bool>> DeliverStopAsync(string courierId, string runId, string orderId, DeliverStopRequestDto request);

        // 12. App Sync Location
        Task<Result<bool>> SyncLocationAsync(string courierId, LocationSyncRequestDto request);

        // 13. Get Earnings (App Requirement)
        Task<Result<CourierEarningsResponseDto>> GetEarningsStatsAsync(string courierId);

        // 7. Delivery history
        Task<Pagination<DeliveryHistoryDto>> GetDeliveryHistoryAsync(
            string courierId, int pageIndex, int pageSize);

        // 8. Stats & Earnings
        Task<Result<CourierStatsDto>> GetStatsAsync(string courierId);

        // 9. Profile
        Task<Result<CourierProfileDto>> GetProfileAsync(string courierId);
        Task<Result<CourierProfileDto>> UpdateProfileAsync(
            string courierId, UpdateCourierProfileRequest request);
    }
}
