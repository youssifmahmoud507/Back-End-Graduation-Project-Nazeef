using LaundryShared.Common;
using LaundryShared.DTO.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Order
{
    public interface IOrderService
    {
        // POST /api/orders
        Task<Result<OrderDto>> PlaceOrderAsync(string customerId, PlaceOrderRequest request);

        // POST /api/orders/calculate-price
        Task<Result<CalculatePriceResponse>> CalculatePriceAsync(CalculatePriceRequest request);

        // GET /api/orders
        Task<Result<Pagination<OrderDto>>> GetCustomerOrdersAsync(string customerId, OrderParams orderParams);

        // GET /api/orders/active
        Task<Result<List<OrderDto>>> GetActiveOrdersAsync(string customerId);

        // GET /api/orders/history
        Task<Result<Pagination<OrderDto>>> GetOrderHistoryAsync(string customerId, OrderParams orderParams);

        // GET /api/orders/{id}
        Task<Result<OrderDto>> GetOrderByIdAsync(string customerId, int orderId);

        // GET /api/orders/{id}/track
        Task<Result<OrderTrackDto>> TrackOrderAsync(string customerId, int orderId);

        // PUT /api/orders/{id}/cancel
        Task<Result<bool>> CancelOrderAsync(string customerId, int orderId, CancelOrderRequest request);

        // GET /api/orders/{id}/payment
        Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId);

    }
}
