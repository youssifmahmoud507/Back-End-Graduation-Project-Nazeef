using System;
using System.Threading.Tasks;
using LaundryShared.Common;
using LaundryShared.DTO.BundleModule;
using LaundryShared.DTO.Order;

namespace LaundryServiesAbstraction.IServices.BundleModule
{
    public interface IBundleOrderService
    {
        Task<Result<OrderDto>> PlaceBundleOrderAsync(PlaceBundleOrderRequest request, string userId);
        Task<BundleOrderMetadataDto?> GetBundleMetadataByOrderAsync(int orderId);
    }
}
